//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignmentRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// EF Core реалізація <see cref="IMissionAssignmentRepository"/>.
/// </summary>
public sealed class MissionAssignmentRepository(IDbContextFactory<AppDbContext> dbFactory)
    : IMissionAssignmentRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<MissionAssignment>> GetPersonAssignmentsAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));
        if (to < from)
            throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= to && (!x.To.HasValue || x.To.Value >= from)) // overlap
            .OrderBy(x => x.From)
            .ThenBy(x => x.MissionId)
            .ThenBy(x => x.CombatTaskDocumentId)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<MissionAssignment?> GetActiveForPersonAsync(
        Guid personId,
        DateOnly onDate,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= onDate && (!x.To.HasValue || x.To.Value >= onDate))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task ApplyPostedCombatTaskDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        if (documentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(documentId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var spans = await db.TimesheetTaskSpans
            .AsNoTracking()
            .Where(s => s.CombatTaskDocumentId == documentId)
            .Where(s => s.Status == DocumentStatus.Posted)
            .Select(s => new
            {
                s.PersonId,
                s.MissionId,
                s.FromDate,
                s.ToDate
            })
            .ToListAsync(ct);

        if (spans.Count == 0)
        {
            await tx.CommitAsync(ct);
            return;
        }

        var existing = await db.MissionAssignments
            .Where(a => a.CombatTaskDocumentId == documentId)
            .ToListAsync(ct);

        var map = existing.ToDictionary(k => (k.MissionId, k.PersonId), v => v);

        foreach (var s in spans)
        {
            var key = (s.MissionId, s.PersonId);

            if (!map.TryGetValue(key, out var a))
            {
                a = new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    CombatTaskDocumentId = documentId,
                    MissionId = s.MissionId,
                    PersonId = s.PersonId,
                    From = s.FromDate,
                    To = s.ToDate,
                    ClosedByDocumentId = null
                };
                db.MissionAssignments.Add(a);
                map[key] = a;
                continue;
            }

            a.From = s.FromDate;

            // To може вже бути закритий аварійно/документом — не “відкриваємо” назад.
            if (a.To is null)
                a.To = s.ToDate;
            else if (s.ToDate is not null && s.ToDate.Value < a.To.Value)
                a.To = s.ToDate.Value;

            // ClosedByDocumentId НЕ чіпаємо тут.
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task CloseAsync(
    Guid startDocumentId,
    Guid missionId,
    Guid personId,
    DateOnly closeAt,
    Guid? closedByDocumentId,
    CancellationToken ct = default)
    {
        if (startDocumentId == Guid.Empty) throw new ArgumentException("startDocumentId is required.", nameof(startDocumentId));
        if (missionId == Guid.Empty) throw new ArgumentException("missionId is required.", nameof(missionId));
        if (personId == Guid.Empty) throw new ArgumentException("personId is required.", nameof(personId));
        if (closeAt == default) throw new ArgumentException("closeAt must be set.", nameof(closeAt));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var a = await db.MissionAssignments
            .Where(x => x.CombatTaskDocumentId == startDocumentId)
            .Where(x => x.MissionId == missionId)
            .Where(x => x.PersonId == personId)
            .FirstOrDefaultAsync(ct);

        if (a is null)
        {
            await tx.CommitAsync(ct);
            return;
        }

        if (closeAt < a.From)
            throw new InvalidOperationException("closeAt must be >= From.");

        if (a.To is null || a.To.Value > closeAt)
            a.To = closeAt;

        // Записуємо документ закриття, якщо він є.
        // Якщо вже є ClosedByDocumentId — не затираємо (перший “винний” лишається).
        if (closedByDocumentId.HasValue && a.ClosedByDocumentId is null)
            a.ClosedByDocumentId = closedByDocumentId.Value;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
