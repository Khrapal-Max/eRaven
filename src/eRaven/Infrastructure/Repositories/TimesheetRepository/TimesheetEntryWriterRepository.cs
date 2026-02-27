//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryWriterRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Timesheet-only writer: додає/коригує/видаляє події <see cref="TimesheetEntry"/> тільки через методи
/// агрегату епізоду <see cref="TimeSheetAggregate"/>.
///
/// <para>
/// Репозиторій не керує інтервалами напряму (не виставляє <see cref="TimesheetEntry.To"/> вручну).
/// Інваріанти підтримує агрегат.
/// </para>
/// </summary>
public sealed class TimesheetEntryWriterRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetEntryWriterRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Domain-level operations
    //======================================================================

    /// <inheritdoc />
    public async Task<Guid> AddEntryAsync(
        Guid personId,
        DateOnly effectiveAt,
        Guid codeId,
        string? reference,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureGuid(personId, nameof(personId));
        EnsureGuid(codeId, nameof(codeId));
        EnsureDate(effectiveAt, nameof(effectiveAt));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ep = await LoadEpisodeForUpdateAsync(db, personId, effectiveAt, ct);

        ep.AddTimesheetEntry(codeId, effectiveAt, reference, author.Trim(), nowUtc, note);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Return created id (by exact From date). Для гарантії унікальності (timesheetId+from) існує індекс.
        var created = FindEntryByFrom(ep, effectiveAt);
        return created?.Id ?? Guid.Empty;
    }

    /// <inheritdoc />
    public async Task CorrectEntryAsync(
        Guid personId,
        Guid entryId,
        Guid nextCodeId,
        DateOnly nextEffectiveAt,
        string? reference,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureGuid(personId, nameof(personId));
        EnsureGuid(entryId, nameof(entryId));
        EnsureGuid(nextCodeId, nameof(nextCodeId));
        EnsureDate(nextEffectiveAt, nameof(nextEffectiveAt));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var (timesheetId, ownerPersonId) = await ResolveEntryOwnerAsync(db, entryId, ct);
        if (ownerPersonId != personId)
            throw new InvalidOperationException("Entry belongs to another person.");

        var ep = await LoadEpisodeByIdForUpdateAsync(db, timesheetId, ct);

        ep.CorrectionTimesheetEntry(entryId, nextCodeId, nextEffectiveAt, reference, author.Trim(), nowUtc, note);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task RemoveEntryAsync(
        Guid personId,
        Guid entryId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureGuid(personId, nameof(personId));
        EnsureGuid(entryId, nameof(entryId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var (timesheetId, ownerPersonId) = await ResolveEntryOwnerAsync(db, entryId, ct);
        if (ownerPersonId != personId)
            throw new InvalidOperationException("Entry belongs to another person.");

        var ep = await LoadEpisodeByIdForUpdateAsync(db, timesheetId, ct);
        ep.RemoveTimesheetEntry(entryId);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task<Guid> TransitionAsync(
        Guid personId,
        DateOnly effectiveAt,
        Guid nextCodeId,
        string? reference,
        string? note,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureGuid(personId, nameof(personId));
        EnsureGuid(nextCodeId, nameof(nextCodeId));
        EnsureDate(effectiveAt, nameof(effectiveAt));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ep = await LoadEpisodeForUpdateAsync(db, personId, effectiveAt, ct);

        // Якщо на дату вже є подія (anchor), робимо replace-in-place.
        // Інакше додаємо нову подію.
        var active = FindActiveEntryOnDate(ep, effectiveAt);

        if (active is not null && active.From == effectiveAt)
        {
            if (active.TimesheetCodeDefinitionId != nextCodeId
                || !string.Equals(active.Reference ?? string.Empty, reference ?? string.Empty, StringComparison.Ordinal))
            {
                ep.CorrectionTimesheetEntry(active.Id, nextCodeId, effectiveAt, reference, author.Trim(), nowUtc, note);
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return active.Id;
        }
        else
        {
            ep.AddTimesheetEntry(nextCodeId, effectiveAt, reference, author.Trim(), nowUtc, note);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        var created = FindEntryByFrom(ep, effectiveAt);
        return created?.Id ?? Guid.Empty;
    }

    /// <inheritdoc />
    public async Task ApplyChangePointsAsync(
        Guid personId,
        IReadOnlyList<(DateOnly EffectiveAt, Guid CodeId, string? Reference)> points,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureGuid(personId, nameof(personId));

        if (points.Count == 0)
            return;

        // normalize input: remove invalid, sort, and keep last point per date
        var ordered = points
            .Where(p => p.EffectiveAt != default && p.CodeId != Guid.Empty)
            .OrderBy(p => p.EffectiveAt)
            .ToList();

        if (ordered.Count == 0)
            return;

        // keep last per EffectiveAt
        var compact = new List<(DateOnly EffectiveAt, Guid CodeId, string? Reference)>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            var p = ordered[i];
            if (compact.Count > 0 && compact[^1].EffectiveAt == p.EffectiveAt)
                compact[^1] = p;
            else
                compact.Add(p);
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ep = await LoadEpisodeForUpdateAsync(db, personId, compact[0].EffectiveAt, ct);

        foreach (var (effectiveAt, codeId, reference) in compact)
        {
            var existing = FindEntryByFrom(ep, effectiveAt);

            if (existing is null)
            {
                ep.AddTimesheetEntry(codeId, effectiveAt, reference, author.Trim(), nowUtc);
                continue;
            }

            var nextRef = reference ?? string.Empty;

            // avoid no-op correction
            if (existing.TimesheetCodeDefinitionId == codeId
                && string.Equals(existing.Reference ?? string.Empty, nextRef, StringComparison.Ordinal))
            {
                continue;
            }

            ep.CorrectionTimesheetEntry(existing.Id, codeId, effectiveAt, nextRef, author.Trim(), nowUtc);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    //======================================================================
    // Legacy / low-level operations
    //======================================================================

    /// <inheritdoc />
    public async Task SoftDeleteAsync(
        Guid entryId,
        string reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureGuid(entryId, nameof(entryId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("reason is required.", nameof(reason));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var (timesheetId, _) = await ResolveEntryOwnerAsync(db, entryId, ct);
        var ep = await LoadEpisodeByIdForUpdateAsync(db, timesheetId, ct);

        // Soft-delete через агрегат.
        ep.SoftDeleteTimesheetEntry(entryId, reason.Trim(), author.Trim(), nowUtc);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TimesheetEntry updated, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updated);
        EnsureGuid(updated.Id, nameof(updated.Id));
        EnsureGuid(updated.TimesheetId, nameof(updated.TimesheetId));
        EnsureGuid(updated.PersonId, nameof(updated.PersonId));
        EnsureGuid(updated.TimesheetCodeDefinitionId, nameof(updated.TimesheetCodeDefinitionId));
        EnsureDate(updated.From, nameof(updated.From));

        if (string.IsNullOrWhiteSpace(updated.UpdatedBy) || !updated.UpdatedAtUtc.HasValue)
        {
            throw new InvalidOperationException(
                "UpdateAsync expects UpdatedBy/UpdatedAtUtc to be set on the provided entity.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ep = await LoadEpisodeByIdForUpdateAsync(db, updated.TimesheetId, ct);

        if (ep.PersonId != updated.PersonId)
            throw new InvalidOperationException("Entry belongs to another person.");

        ep.CorrectionTimesheetEntry(
            entyId: updated.Id,
            nextCodeId: updated.TimesheetCodeDefinitionId,
            fromEffectiveAt: updated.From,
            reference: updated.Reference,
            author: updated.UpdatedBy!.Trim(),
            nowUtc: updated.UpdatedAtUtc.Value);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task SaveTransitionAsync(TimesheetEntry prevUpdated, TimesheetEntry nextAdded, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(prevUpdated);
        ArgumentNullException.ThrowIfNull(nextAdded);

        EnsureGuid(prevUpdated.Id, nameof(prevUpdated.Id));
        EnsureGuid(nextAdded.Id, nameof(nextAdded.Id));
        EnsureGuid(prevUpdated.TimesheetId, nameof(prevUpdated.TimesheetId));
        EnsureGuid(nextAdded.TimesheetId, nameof(nextAdded.TimesheetId));

        if (prevUpdated.TimesheetId != nextAdded.TimesheetId)
            throw new InvalidOperationException("prevUpdated and nextAdded must belong to the same timesheet episode.");

        if (string.IsNullOrWhiteSpace(prevUpdated.UpdatedBy) || !prevUpdated.UpdatedAtUtc.HasValue)
            throw new InvalidOperationException("SaveTransitionAsync expects prevUpdated.UpdatedBy/UpdatedAtUtc to be set.");

        if (string.IsNullOrWhiteSpace(nextAdded.CreatedBy) || nextAdded.CreatedAtUtc == default)
            throw new InvalidOperationException("SaveTransitionAsync expects nextAdded.CreatedBy/CreatedAtUtc to be set.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var ep = await LoadEpisodeByIdForUpdateAsync(db, prevUpdated.TimesheetId, ct);

        // prevUpdated: touch audit through aggregate (no manual To changes).
        ep.TouchEntryAudit(prevUpdated.Id, prevUpdated.UpdatedBy!.Trim(), prevUpdated.UpdatedAtUtc.Value);

        // nextAdded: add new change-point with provided Id (legacy contract requires stable id).
        ep.AddTimesheetEntry(
            entryId: nextAdded.Id,
            nextCodeId: nextAdded.TimesheetCodeDefinitionId,
            effectiveAt: nextAdded.From,
            reference: nextAdded.Reference,
            note: nextAdded.Note,
            author: nextAdded.CreatedBy.Trim(),
            nowUtc: nextAdded.CreatedAtUtc);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static void EnsureAuthor(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("author is required.", nameof(author));
    }

    private static void EnsureGuid(Guid id, string paramName)
    {
        if (id == Guid.Empty)
            throw new ArgumentException($"{paramName} is required.", paramName);
    }

    private static void EnsureDate(DateOnly d, string paramName)
    {
        if (d == default)
            throw new ArgumentException($"{paramName} is required.", paramName);
    }

    private static async Task<TimeSheetAggregate> LoadEpisodeForUpdateAsync(
        AppDbContext db,
        Guid personId,
        DateOnly onDate,
        CancellationToken ct)
    {
        var ep = await db.TimeSheets
            .Include(t => t.Entries)
            .SingleOrDefaultAsync(t =>
                t.PersonId == personId
                && t.OpenedAt <= onDate
                && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= onDate), ct);

        return ep ?? throw new InvalidOperationException("Timesheet episode not found for the specified date.");
    }

    private static async Task<TimeSheetAggregate> LoadEpisodeByIdForUpdateAsync(AppDbContext db, Guid timesheetId, CancellationToken ct)
    {
        var ep = await db.TimeSheets
            .Include(t => t.Entries)
            .SingleOrDefaultAsync(t => t.Id == timesheetId, ct);

        return ep ?? throw new InvalidOperationException("Timesheet episode not found.");
    }

    private static TimesheetEntry? FindActiveEntryOnDate(TimeSheetAggregate ep, DateOnly d)
    {
        // half-open: From <= d && (To == null || d < To)
        TimesheetEntry? best = null;

        foreach (var e in ep.Entries)
        {
            if (e.IsDeleted) continue;
            if (e.From > d) continue;
            if (e.To.HasValue && d >= e.To.Value) continue;

            if (best is null || e.From > best.From)
                best = e;
        }

        return best;
    }

    private static TimesheetEntry? FindEntryByFrom(TimeSheetAggregate ep, DateOnly from)
    {
        foreach (var e in ep.Entries)
        {
            if (!e.IsDeleted && e.From == from)
                return e;
        }
        return null;
    }

    private static async Task<(Guid TimesheetId, Guid PersonId)> ResolveEntryOwnerAsync(
        AppDbContext db,
        Guid entryId,
        CancellationToken ct)
    {
        var row = await db.TimesheetEntries
            .AsNoTracking()
            .Where(e => !e.IsDeleted)
            .Where(e => e.Id == entryId)
            .Select(e => new { e.TimesheetId, e.PersonId })
            .SingleOrDefaultAsync(ct);

        return row is null ? throw new InvalidOperationException("Timesheet entry not found.") : (row.TimesheetId, row.PersonId);
    }
}
