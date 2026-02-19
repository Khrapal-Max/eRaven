//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMissionPlanningRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.CombatTasks;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-репозиторій для планування/звітів по місіях.
/// Джерело правди — факти табеля: <c>TimesheetTaskSpans</c> + поточний код з <c>TimesheetEntries</c>.
/// </summary>
public sealed class TimesheetMissionPlanningRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetMissionPlanningRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetTaskSpan>> GetActiveMissionPersonsAsync(
        Guid missionId,
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (missionId == Guid.Empty)
            throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Half-open interval: [FromDate..ToDate)
        return await db.TimesheetTaskSpans
            .AsNoTracking()
            .Where(s => s.MissionId == missionId)
            .Where(s => s.Status != DocumentStatus.Canceled)
            .Where(s => s.FromDate <= onDate && (!s.ToDate.HasValue || onDate < s.ToDate.Value))
            .OrderBy(s => s.FullName)
            .ThenBy(s => s.FromDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetTaskSpan>> GetActiveMissionClosablePersonsAsync(
       Guid missionId,
       DateOnly onDate,
       CancellationToken ct = default)
    {
        if (missionId == Guid.Empty)
            throw new ArgumentException("missionId must be set.", nameof(missionId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetTaskSpans
            .AsNoTracking()
            .Where(s => s.MissionId == missionId)
            .Where(s => s.Status != DocumentStatus.Canceled)
            .Where(s => s.FromDate <= onDate && (!s.ToDate.HasValue || onDate < s.ToDate.Value))
            .Where(s => s.ClosedByCombatTaskDocumentId == null && s.ClosedByCodeId == null)
            .OrderBy(s => s.FullName)
            .ThenBy(s => s.FromDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetTaskSpan>> GetActiveMissionPersonsByDocumentAsync(
        Guid documentId,
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("documentId must be set.", nameof(documentId));
        if (onDate == default)
            throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetTaskSpans
            .AsNoTracking()
            .Where(s => s.Status != DocumentStatus.Canceled)
            .Where(s => s.FromDate <= onDate && (!s.ToDate.HasValue || onDate < s.ToDate.Value))
            .Where(s => s.OpenedByCombatTaskDocumentId == documentId || s.ClosedByCombatTaskDocumentId == documentId)
            .OrderBy(s => s.FullName)
            .ThenBy(s => s.FromDate)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReadyCombatTaskPersonDto>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (onDate == default) throw new ArgumentException("onDate must be set.", nameof(onDate));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var readyCodeId = await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.Code == TimesheetSystemCodes.ReadyToCombatTask)
            .Select(x => x.Id)
            .SingleAsync(ct);

        var q = db.PersonRead
            .AsNoTracking()
            .Select(p => new
            {
                Person = p,

                TimesheetId = db.TimeSheets
                    .AsNoTracking()
                    .Where(t =>
                        t.PersonId == p.Id
                        && t.OpenedAt <= onDate
                        && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= onDate))
                    .OrderByDescending(t => t.OpenedAt)
                    .Select(t => t.Id)
                    .FirstOrDefault()
            })
            .Where(x => x.TimesheetId != Guid.Empty)
            .Select(x => new
            {
                x.Person,
                x.TimesheetId,

                CurrentCodeId = db.TimesheetEntries
                    .AsNoTracking()
                    .Where(e =>
                        e.TimesheetId == x.TimesheetId
                        && !e.IsDeleted
                        && e.From <= onDate
                        && (!e.To.HasValue || onDate < e.To.Value))
                    .OrderByDescending(e => e.From)
                    .ThenByDescending(e => e.CreatedAtUtc)
                    .Select(e => e.TimesheetCodeDefinitionId)
                    .FirstOrDefault(),

                HasActiveTask = db.TimesheetTaskSpans
                    .AsNoTracking()
                    .Any(s =>
                        s.TimesheetId == x.TimesheetId
                        && s.Status != DocumentStatus.Canceled
                        && s.FromDate <= onDate
                        && (!s.ToDate.HasValue || onDate < s.ToDate.Value))
            })
            .Where(x => x.CurrentCodeId == readyCodeId && !x.HasActiveTask)
            .OrderBy(x => x.Person.FullName)
            .Select(x => new ReadyCombatTaskPersonDto(
                PersonId: x.Person.Id,
                Rnokpp: x.Person.Rnokpp,
                FullName: x.Person.FullName,
                Rank: x.Person.Rank,
                Position: x.Person.Position,
                Weapon: x.Person.Weapon,
                Callsign: x.Person.Callsign));

        return await q.ToListAsync(ct);
    }
}
