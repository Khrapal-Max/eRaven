//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Write/CRUD репозиторій табельних фактів (<see cref="TimesheetEntry"/>).
///
/// Примітки:
/// <list type="bullet">
/// <item><description>Soft-delete: записи не видаляємо фізично; всі read-методи ігнорують <see cref="TimesheetEntry.IsDeleted"/>.</description></item>
/// <item><description>Інтервали entry — half-open: <c>[From..To)</c> (To exclusive). <c>To == null</c> => open-ended.</description></item>
/// </list>
/// </summary>
public sealed class TimesheetEntryRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetEntryRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonsAsync(
        IReadOnlyCollection<Guid> personIds,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        //TODO не задіяний
        if (personIds.Count == 0) return [];
        if (to < from) throw new ArgumentException("To must be >= From.", nameof(to));

        var toExclusive = to.AddDays(1);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => personIds.Contains(x.PersonId))
            .Where(x => x.From < toExclusive && (!x.To.HasValue || x.To.Value > from)) // overlap with [from..to]
            .OrderBy(x => x.PersonId)
            .ThenBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetEntry>> GetEntriesForPersonAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        //TODO не задіяний
        if (to < from) throw new ArgumentException("To must be >= From.", nameof(to));

        var toExclusive = to.AddDays(1);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From < toExclusive && (!x.To.HasValue || x.To.Value > from)) // overlap with [from..to]
            .OrderBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TimesheetEntry?> GetByIdAsync(Guid entryId, CancellationToken ct = default)
    {
        //TODO не задіяний
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entryId && !x.IsDeleted, ct);
    }

    /// <inheritdoc />
    public async Task<TimesheetEntry?> GetNextEntryAfterDateAsync(
        Guid timesheetId,
        Guid personId,
        DateOnly date,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimesheetId == timesheetId)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From > date)
            .OrderBy(x => x.From)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TimesheetEntry?> GetActiveEntryOnDateAsync(
        Guid timesheetId,
        Guid personId,
        DateOnly date,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // half-open: From <= date && (To == null || date < To)
        return await db.TimesheetEntries
            .AsNoTracking()
            .Include(x => x.TimesheetCodeDefinition)
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimesheetId == timesheetId)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= date && (!x.To.HasValue || date < x.To.Value))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(
        Guid entryId,
        string reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        //TODO не задіяний
        if (entryId == Guid.Empty) throw new ArgumentException("EntryId is required.", nameof(entryId));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.", nameof(reason));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var entry = await db.TimesheetEntries.FirstOrDefaultAsync(x => x.Id == entryId, ct);
        if (entry is null || entry.IsDeleted) return;

        entry.IsDeleted = true;
        entry.DeletedBy = string.IsNullOrWhiteSpace(author) ? "system" : author.Trim();
        entry.DeletedAtUtc = nowUtc;
        entry.DeleteReason = reason.Trim();

        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TimesheetEntry updated, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(updated);
        if (updated.Id == Guid.Empty) throw new ArgumentException("EntryId is required.", nameof(updated));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var tl = await LoadTimelineForInsertAsync(db, updated.TimesheetId, ct);
        EnsureEntryWithinTimeline(tl, updated);

        // IMPORTANT:
        // Never DbSet.Update(updated) on detached graphs (can overwrite FK back to navigation).
        var existing = await db.TimesheetEntries
            .FirstOrDefaultAsync(x => x.Id == updated.Id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Timesheet entry not found.");

        if (existing.TimesheetId != updated.TimesheetId)
            throw new InvalidOperationException("Entry timesheet mismatch.");

        existing.TimesheetCodeDefinitionId = updated.TimesheetCodeDefinitionId;
        existing.From = updated.From;
        existing.To = updated.To;
        existing.Reference = updated.Reference;
        existing.Note = updated.Note;
        existing.UpdatedBy = updated.UpdatedBy;
        existing.UpdatedAtUtc = updated.UpdatedAtUtc;

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task SaveTransitionAsync(TimesheetEntry prevUpdated, TimesheetEntry nextAdded, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var tl = await LoadTimelineForInsertAsync(db, prevUpdated.TimesheetId, ct);

        if (nextAdded.TimesheetId != prevUpdated.TimesheetId)
            throw new InvalidOperationException("Перехід повинен виконуватись в межах одного епізоду табеля.");

        EnsureEntryWithinTimeline(tl, prevUpdated);
        EnsureEntryWithinTimeline(tl, nextAdded);

        var prev = await db.TimesheetEntries
            .FirstOrDefaultAsync(x => x.Id == prevUpdated.Id && !x.IsDeleted, ct)
            ?? throw new InvalidOperationException("Previous timesheet entry not found.");

        prev.To = prevUpdated.To;
        prev.UpdatedBy = prevUpdated.UpdatedBy;
        prev.UpdatedAtUtc = prevUpdated.UpdatedAtUtc;

        db.TimesheetEntries.Add(nextAdded);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// Завантажує епізод табеля для write-вставки/переходу та застосовує правило:
    /// якщо епізод закритий — вставки/переходи заборонені.
    /// </summary>
    private static async Task<TimeSheetAggregate> LoadTimelineForInsertAsync(
        AppDbContext db,
        Guid timesheetId,
        CancellationToken ct)
    {
        var tl = await db.TimeSheets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == timesheetId, ct)
            ?? throw new InvalidOperationException("Епізод табеля не знайдено (дані пошкоджені).");

        if (tl.ClosedAt.HasValue)
            throw new InvalidOperationException(
                $"Епізод табеля закритий {tl.ClosedAt.Value:yyyy-MM-dd}. Вставки/переходи заборонені.");

        return tl;
    }

    private static void EnsureEntryWithinTimeline(TimeSheetAggregate tl, TimesheetEntry e)
    {
        if (e.TimesheetId != tl.Id)
            throw new InvalidOperationException("TimesheetEntry.TimesheetId не відповідає епізоду табеля.");

        if (e.PersonId != tl.PersonId)
            throw new InvalidOperationException("TimesheetEntry.PersonId не відповідає власнику епізоду табеля.");

        if (e.From < tl.OpenedAt)
            throw new InvalidOperationException(
                $"Запис не може починатися раніше OpenedAt ({tl.OpenedAt:yyyy-MM-dd}).");

        // half-open: To (exclusive) must be strictly greater than From.
        if (e.To.HasValue && e.To.Value <= e.From)
            throw new InvalidOperationException("Некоректний період: To (exclusive) повинен бути > From.");

        // p.2: якщо епізод колись буде закритий — тут залишаємо перевірку меж закриття.
        // (Зараз вставки в закритий епізод заборонені, але update (підрізання) може виконуватись в інших сценаріях.)
        if (tl.ClosedAt.HasValue)
        {
            if (!e.To.HasValue)
                throw new InvalidOperationException(
                    $"Open-ended запис заборонений у закритому епізоді (ClosedAt={tl.ClosedAt:yyyy-MM-dd}).");

            // last day in closed episode is ClosedAt (inclusive), so To(exclusive) may be ClosedAt+1.
            if (e.To.Value > tl.ClosedAt.Value.AddDays(1))
                throw new InvalidOperationException(
                    $"Запис виходить за межі закритого епізоду (ClosedAt={tl.ClosedAt:yyyy-MM-dd}).");
        }
    }
}
