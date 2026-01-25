//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskReadRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Read-repo для сторінок планування.
/// На цьому кроці — інфраструктура (порожні результати).
/// Далі підв’яжемо до реальних таблиць/сутностей (Documents/Lines/Assignments).
/// </summary>
public sealed class CombatTaskReadRepository(IDbContextFactory<AppDbContext> dbFactory) : ICombatTaskReadRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<PlanningMonthAssignmentRowDto>> GetPlanningMonthAsync(
        int year,
        int month,
        string? search,
        CancellationToken ct = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var s = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var q =
            from a in db.Set<CombatTaskAssignment>().AsNoTracking()
            join d in db.Set<CombatTaskPlanDocument>().AsNoTracking()
                on a.StartDocumentId equals d.Id
            where d.Status != CombatTaskPlanDocumentStatus.Canceled
            // overlap with month
            where a.StartedAt <= monthEnd && (a.EndedAt == null || a.EndedAt.Value >= monthStart)
            select new { a, d };

        if (s is not null)
        {
            q = q.Where(x =>
                x.a.FullName.Contains(s) ||
                x.a.RNOKPP.Contains(s));
        }

        return await q
            .OrderBy(x => x.a.StartedAt)
            .ThenBy(x => x.a.FullName)
            .ThenBy(x => x.a.PersonId)
            .Select(x => new PlanningMonthAssignmentRowDto(
                AssignmentId: x.a.Id,
                PersonId: x.a.PersonId,
                FullName: x.a.FullName,
                RNOKPP: x.a.RNOKPP,
                Rank: x.a.Rank,
                Position: x.a.Position,
                Weapon: x.a.Weapon,
                Callsign: x.a.Callsign,

                PlanningDate: x.a.PlanningDate,
                PlanningDocTitle: x.a.PlanningDocTitle,

                StartDate: x.a.StartedAt,
                EndDate: x.a.EndedAt,

                PositionalArea: x.a.PositionalArea,
                GroupName: x.a.GroupName,
                AssetType: x.a.AssetType,
                Mode: x.a.Mode.ToString(),
                Goal: x.a.Goal,

                IsActual: x.a.EndedAt == null,              // для UI (діюче = не закрито)
                DocumentStatus: x.d.Status
            ))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlanningDocumentRowDto>> GetPlanningDocumentsAsync(
        int year,
        int month,
        CombatTaskPlanDocumentStatus? status,
        string? search,
        CancellationToken ct = default)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var s = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var docs = db.Set<CombatTaskPlanDocument>()
            .AsNoTracking()
            .Where(d => d.PlanningDate >= monthStart && d.PlanningDate <= monthEnd);

        if (status is not null)
            docs = docs.Where(d => d.Status == status.Value);

        if (s is not null)
            docs = docs.Where(d => d.PlanningDocTitle.Contains(s));

        // підрахунки робимо через Assignments (StartDocumentId або EndDocumentId)
        var assignments = db.Set<CombatTaskAssignment>().AsNoTracking();

        return await docs
            .OrderByDescending(d => d.PlanningDate)
            .ThenByDescending(d => d.RecordedAt)
            .Select(d => new PlanningDocumentRowDto(
                DocumentId: d.Id,
                DocumentDate: d.PlanningDate,
                Title: d.PlanningDocTitle,
                Status: d.Status,

                PersonsCount: assignments
                    .Where(a => a.StartDocumentId == d.Id || a.EndDocumentId == d.Id)
                    .Select(a => a.PersonId)
                    .Distinct()
                    .Count(),

                MinStart: assignments
                    .Where(a => a.StartDocumentId == d.Id || a.EndDocumentId == d.Id)
                    .Min(a => (DateOnly?)a.StartedAt),

                MaxEnd: assignments
                    .Where(a => a.StartDocumentId == d.Id || a.EndDocumentId == d.Id)
                    .Max(a => a.EndedAt),

                CreatedAtUtc: d.CreatedAtUtc
            ))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PlanningDayGroupDto>> GetPlanningDayAsync(
        DateOnly date,
        string? search,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var s = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var q =
            from a in db.Set<CombatTaskAssignment>().AsNoTracking()
            join d in db.Set<CombatTaskPlanDocument>().AsNoTracking()
                on a.StartDocumentId equals d.Id
            where d.Status != CombatTaskPlanDocumentStatus.Canceled
            where a.StartedAt <= date && (a.EndedAt == null || a.EndedAt.Value >= date)
            select new { a, d };

        if (s is not null)
        {
            q = q.Where(x =>
                x.a.FullName.Contains(s) ||
                x.a.RNOKPP.Contains(s));
        }

        var flat = await q
            .OrderBy(x => x.a.PositionalArea)
            .ThenBy(x => x.a.GroupName)
            .ThenBy(x => x.a.AssetType)
            .ThenBy(x => x.a.Mode)
            .ThenBy(x => x.a.Goal)
            .ThenBy(x => x.a.FullName)
            .Select(x => new
            {
                x.a.PositionalArea,
                x.a.GroupName,
                x.a.AssetType,
                x.a.Mode,
                x.a.Goal,

                x.a.PersonId,
                x.a.FullName,
                x.a.RNOKPP,
                x.a.Rank,
                x.a.Position,
                x.a.Callsign,
                x.a.StartedAt,
                x.a.EndedAt,
                DocumentTitle = x.a.PlanningDocTitle
            })
            .ToListAsync(ct);

        return [.. flat
            .GroupBy(x => new { x.PositionalArea, x.GroupName, x.AssetType, x.Mode, x.Goal })
            .Select(g => new PlanningDayGroupDto(
                PositionalArea: g.Key.PositionalArea,
                GroupName: g.Key.GroupName,
                AssetType: g.Key.AssetType,
                Mode: g.Key.Mode.ToString(),
                Goal: g.Key.Goal,
                Persons: [.. g.Select(p => new PlanningDayPersonDto(
                    PersonId: p.PersonId,
                    FullName: p.FullName,
                    RNOKPP: p.RNOKPP,
                    Rank: p.Rank,
                    Position: p.Position,
                    Callsign: p.Callsign,
                    StartDate: p.StartedAt,
                    EndDate: p.EndedAt,
                    DocumentTitle: p.DocumentTitle
                ))]
            ))
            .OrderBy(x => x.PositionalArea)
            .ThenBy(x => x.GroupName)
            .ThenBy(x => x.AssetType)
            .ThenBy(x => x.Mode)
            .ThenBy(x => x.Goal)];
    }
}