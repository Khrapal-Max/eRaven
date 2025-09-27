using eRaven.Domain.Models;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.StaffingAggregation;

public class StaffingAggregationService(IDbContextFactory<AppDbContext> dbf) : IStaffingAggregationService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;

    public async Task<IReadOnlyList<StaffingPersonTimeline>> BuildTimelineAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var startUtc = EnsureUtc(fromUtc);
        var endUtc = EnsureUtc(toUtc);

        if (startUtc >= endUtc)
            return Array.Empty<StaffingPersonTimeline>();

        await using var db = await _dbf.CreateDbContextAsync(ct);

        var persons = await db.Persons
            .AsNoTracking()
            .Include(p => p.StatusKind)
            .Include(p => p.PositionUnit)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ThenBy(p => p.MiddleName)
            .ToListAsync(ct);

        if (persons.Count == 0)
            return Array.Empty<StaffingPersonTimeline>();

        var personIds = persons.Select(p => p.Id).ToHashSet();

        var statuses = await db.PersonStatuses
            .AsNoTracking()
            .Include(s => s.StatusKind)
            .Where(s => personIds.Contains(s.PersonId) && s.OpenDate < endUtc)
            .ToListAsync(ct);

        var assignments = await db.PersonPositionAssignments
            .AsNoTracking()
            .Include(a => a.PositionUnit)
            .Where(a => personIds.Contains(a.PersonId) && a.OpenUtc < endUtc && (a.CloseUtc == null || a.CloseUtc > startUtc))
            .ToListAsync(ct);

        var plans = await db.PlanActions
            .AsNoTracking()
            .Where(p => personIds.Contains(p.PersonId) && p.EffectiveAtUtc >= startUtc && p.EffectiveAtUtc < endUtc)
            .OrderBy(p => p.EffectiveAtUtc)
            .ToListAsync(ct);

        var statusesByPerson = statuses.GroupBy(s => s.PersonId).ToDictionary(g => g.Key, g => g.OrderBy(s => s.OpenDate).ThenBy(s => s.Sequence).ToList());
        var assignmentsByPerson = assignments.GroupBy(a => a.PersonId).ToDictionary(g => g.Key, g => g.OrderBy(a => a.OpenUtc).ToList());
        var plansByPerson = plans.GroupBy(p => p.PersonId).ToDictionary(g => g.Key, g => g.ToList());

        var builder = new StaffingTimelineBuilder(startUtc, endUtc);
        var result = new List<StaffingPersonTimeline>(persons.Count);

        foreach (var person in persons)
        {
            statusesByPerson.TryGetValue(person.Id, out var personStatuses);
            assignmentsByPerson.TryGetValue(person.Id, out var personAssignments);
            plansByPerson.TryGetValue(person.Id, out var personPlans);

            var events = CreateEvents(
                person,
                personStatuses ?? Array.Empty<PersonStatus>(),
                personAssignments ?? Array.Empty<PersonPositionAssignment>(),
                personPlans ?? Array.Empty<PlanAction>(),
                startUtc,
                endUtc);

            var segments = builder.Build(person.Id, events);
            result.Add(new StaffingPersonTimeline(person, segments, personPlans ?? Array.Empty<PlanAction>()));
        }

        return result;
    }

    private static IReadOnlyList<StaffingEvent> CreateEvents(
        Person person,
        IReadOnlyList<PersonStatus> statuses,
        IReadOnlyList<PersonPositionAssignment> assignments,
        IReadOnlyList<PlanAction> plans,
        DateTime fromUtc,
        DateTime toUtc)
    {
        var events = new List<StaffingEvent>();

        var baselineStatus = statuses
            .Where(s => s.OpenDate <= fromUtc)
            .OrderBy(s => s.OpenDate)
            .ThenBy(s => s.Sequence)
            .LastOrDefault();

        var baselineAssignment = assignments
            .Where(a => a.OpenUtc <= fromUtc && (a.CloseUtc is null || a.CloseUtc > fromUtc))
            .OrderBy(a => a.OpenUtc)
            .LastOrDefault();

        var baselineStatusKind = baselineStatus?.StatusKind ?? person.StatusKind;
        var baselineNote = baselineStatus?.Note;
        var baselinePosition = baselineAssignment?.PositionUnit ?? person.PositionUnit;

        if (baselineStatusKind is not null || baselinePosition is not null)
        {
            events.Add(StaffingEvent.CreateBaseline(person.Id, fromUtc, baselineStatusKind, baselineNote, baselinePosition));
        }

        foreach (var status in statuses.Where(s => s.OpenDate >= fromUtc && s.OpenDate < toUtc))
        {
            events.Add(StaffingEvent.CreateStatusChanged(status));
        }

        foreach (var assignment in assignments)
        {
            if (assignment.OpenUtc < toUtc)
                events.Add(StaffingEvent.CreatePositionAssigned(assignment));

            if (assignment.CloseUtc is { } closeUtc && closeUtc >= fromUtc && closeUtc < toUtc)
                events.Add(StaffingEvent.CreatePositionReleased(assignment.PersonId, closeUtc));
        }

        foreach (var plan in plans)
            events.Add(StaffingEvent.CreatePlanAction(plan));

        return events;
    }

    private static DateTime EnsureUtc(DateTime value)
        => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
