//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public sealed class TimesheetRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<Guid> CreateEntryAsync(Guid personId, TimesheetLane lane, string code, DateOnly from, DateOnly? to,
        string? reference, string? note, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureCode(code);

        if (to is DateOnly t && t < from)
            throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        await EnsureNoOverlapAsync(db, entryId: null, personId, lane, from, to, ct);

        var id = Guid.NewGuid();

        var entry = new TimesheetEntry
        {
            Id = id,
            PersonId = personId,
            Lane = lane,
            Code = code.Trim(),
            From = from,
            To = to,
            Reference = Normalize(reference),
            Note = Normalize(note),
            CreatedBy = author.Trim(),
            CreatedAtUtc = nowUtc,
            IsDeleted = false
        };

        db.TimesheetEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        return id;
    }

    public async Task UpdateEntryAsync(Guid entryId, TimesheetLane lane, string code, DateOnly from, DateOnly? to,
        string? reference, string? note, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureCode(code);

        if (entryId == Guid.Empty)
            throw new ArgumentException("EntryId is required.", nameof(entryId));

        if (to is DateOnly t && t < from)
            throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entry = await db.TimesheetEntries
            .FirstOrDefaultAsync(x => x.Id == entryId, ct);

        if (entry is null || entry.IsDeleted)
            throw new InvalidOperationException("Entry not found.");

        await EnsureNoOverlapAsync(db, entryId, entry.PersonId, lane, from, to, ct);

        entry.Lane = lane;
        entry.Code = code.Trim();
        entry.From = from;
        entry.To = to;
        entry.Reference = Normalize(reference);
        entry.Note = Normalize(note);
        entry.UpdatedBy = author.Trim();
        entry.UpdatedAtUtc = nowUtc;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteEntryAsync(Guid entryId, string reason, string author, DateTime nowUtc, CancellationToken ct = default)
    {
        EnsureAuthor(author);

        if (entryId == Guid.Empty)
            throw new ArgumentException("EntryId is required.", nameof(entryId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entry = await db.TimesheetEntries
            .FirstOrDefaultAsync(x => x.Id == entryId, ct);

        if (entry is null || entry.IsDeleted)
            return;

        entry.IsDeleted = true;
        entry.DeletedBy = author.Trim();
        entry.DeletedAtUtc = nowUtc;
        entry.DeleteReason = reason.Trim();

        await db.SaveChangesAsync(ct);
    }

    public async Task<TimesheetEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entryId && !x.IsDeleted, ct);
    }

    public async Task<IReadOnlyList<TimesheetEntry>> GetPersonEntriesAsync(Guid personId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        if (to < from)
            throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && !x.IsDeleted)
            .Where(x => x.From <= to && (x.To == null || x.To >= from)) // overlap with [from..to]
            .OrderBy(x => x.Lane)
            .ThenBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TimesheetEntry>> GetActiveEntriesForTimesheetOnDateAsync(DateOnly date, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // “в табелі на дату” = по EnrolledAt/ExcludedAt (це виправляє кейс з Reserved)
        var activePersonIds = await db.PersonRead
            .AsNoTracking()
            .Where(p =>
                p.EnrolledAt != null &&
                p.EnrolledAt <= date &&
                (p.ExcludedAt == null || p.ExcludedAt >= date))
            .Select(p => p.Id)
            .ToListAsync(ct);

        if (activePersonIds.Count == 0)
            return [];

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => activePersonIds.Contains(x.PersonId))
            .Where(x => x.From <= date && (x.To == null || x.To >= date))
            .OrderBy(x => x.PersonId)
            .ThenBy(x => x.Lane)
            .ThenBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    // =========================
    // Overlap rule
    // =========================

    private static async Task EnsureNoOverlapAsync(AppDbContext db, Guid? entryId, Guid personId, TimesheetLane lane,
        DateOnly from, DateOnly? to, CancellationToken ct)
    {
        var q = db.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.Lane == lane && !x.IsDeleted);

        if (entryId is Guid id)
            q = q.Where(x => x.Id != id);

        // overlap with [from..to] (to null = infinity)
        if (to is DateOnly t)
        {
            q = q.Where(x => x.From <= t && (x.To == null || x.To >= from));
        }
        else
        {
            q = q.Where(x => x.To == null || x.To >= from);
        }

        var hasOverlap = await q.AnyAsync(ct);
        if (hasOverlap)
            throw new InvalidOperationException("Запис перетинається з існуючим записом у цій lane. Спочатку відредагуйте/закрийте попередній запис.");
    }

    private static void EnsureAuthor(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));
    }

    private static void EnsureCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));
    }

    private static string? Normalize(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
