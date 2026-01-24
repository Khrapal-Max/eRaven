//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthGridRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-repo: збирає місячну матрицю табеля (UI grid).
/// Оптимізація: не тягнемо timelineIds в памʼять — використовуємо subqueries/join.
/// </summary>
public sealed class TimesheetMonthGridRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetMonthGridRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<TimesheetMonthGridDto> GetTimesheetMonthAsync(
        int year,
        int month,
        string? search,
        CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year));
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, daysInMonth);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) “Хто в табелі в цьому місяці” => по MAIN timeline (перетин з місяцем)
        var mainInMonthQ = Overlapping(
            db.TimesheetTimelines.AsNoTracking().Where(t => t.Lane == TimesheetLane.Main),
            monthStart, monthEnd);

        // PersonIds як сабквері (не ToList)
        var personIdsQ = mainInMonthQ
            .Select(t => t.PersonId)
            .Distinct();

        // 2) Люди (з пошуком) — теж як query
        var personsQ = db.PersonRead.AsNoTracking()
            .Where(p => personIdsQ.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpperInvariant();
            personsQ = personsQ.Where(p =>
                (p.FullName ?? "").ToUpper().Contains(s) ||
                (p.Rnokpp ?? "").ToUpper().Contains(s));
        }

        var persons = await personsQ
            .OrderBy(p => p.EnrollmentKind)
            .ThenBy(p => p.PositionSort)
            .ThenBy(p => p.FullName)
            .Select(p => new
            {
                p.Id,
                p.FullName,
                p.Rnokpp,
                p.Rank,
                p.Position,
                p.EnrollmentKind,
                p.EnrolledAt,
                p.ExcludedAt
            })
            .ToListAsync(ct);

        if (persons.Count == 0)
            return new TimesheetMonthGridDto(year, month, daysInMonth, []);

        // 3) Таймлайни (MAIN+TASK), але тільки для відфільтрованих осіб і тільки ті що перетинають місяць
        var selectedPersonIds = persons.Select(x => x.Id).ToArray();

        var timelinesInMonthQ = Overlapping(
            db.TimesheetTimelines.AsNoTracking()
                .Where(t => selectedPersonIds.Contains(t.PersonId)),
            monthStart, monthEnd);

        // 4) Entries, що перетинають місяць + належать до timelinesInMonthQ (JOIN, а не IN по id-списку)
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in timelinesInMonthQ on e.TimelineId equals t.Id
            where !e.IsDeleted
                  && e.From <= monthEnd
                  && (!e.To.HasValue || e.To.Value >= monthStart)
            orderby e.PersonId, e.Lane, e.From, e.Id
            select e
        ).ToListAsync(ct);

        var byPerson = entries
            .GroupBy(e => e.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5) Будуємо матрицю (Main default = "НБ", Task default = "")
        var rows = new List<TimesheetMonthPersonRowDto>(persons.Count);

        foreach (var p in persons)
        {
            var main = new string[daysInMonth];
            var task = new string[daysInMonth];

            for (var i = 0; i < daysInMonth; i++)
            {
                main[i] = "НБ";
                task[i] = "";
            }

            if (byPerson.TryGetValue(p.Id, out var list))
            {
                foreach (var e in list)
                {
                    var start = e.From < monthStart ? monthStart : e.From;
                    var end = e.To is null
                        ? monthEnd
                        : (e.To.Value > monthEnd ? monthEnd : e.To.Value);

                    for (var d = start; d <= end; d = d.AddDays(1))
                    {
                        var idx = d.Day - 1;
                        if (idx < 0 || idx >= daysInMonth) continue;

                        var code = (e.Code ?? "").Trim();

                        if (e.Lane == TimesheetLane.Main)
                            main[idx] = code;
                        else if (e.Lane == TimesheetLane.Task)
                            task[idx] = code;
                    }
                }
            }

            rows.Add(new TimesheetMonthPersonRowDto(
                PersonId: p.Id,
                FullName: p.FullName ?? "",
                RNOKPP: p.Rnokpp ?? "",
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                MainCodes: main,
                TaskCodes: task
            ));
        }

        return new TimesheetMonthGridDto(year, month, daysInMonth, rows);
    }

    private static IQueryable<TimesheetTimeline> Overlapping(
        IQueryable<TimesheetTimeline> q,
        DateOnly from,
        DateOnly to)
        => q.Where(t => t.OpenedAt <= to && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= from));
}
