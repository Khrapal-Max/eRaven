//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Write/CRUD репозиторій табельних фактів (<see cref="TimesheetEntry"/>).
///
/// Примітки:
/// - Soft-delete: записи не видаляємо фізично; всі read-методи ігнорують IsDeleted.
/// - Overlap: entry активний на інтервалі [From..To] (To == null => open-ended).
/// </summary>
public sealed class TimesheetEntryRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetEntryRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

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
        Guid timelineId,
        Guid personId,
        DateOnly date,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimelineId == timelineId)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From > date)
            .OrderBy(x => x.From)
            .ThenBy(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetEntry>> GetPersonEntriesAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        //TODO не задіяний
        if (to < from) throw new ArgumentException("To must be >= From.", nameof(to));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= to && (!x.To.HasValue || x.To.Value >= from)) // overlap
            .OrderBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

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

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => personIds.Contains(x.PersonId))
            .Where(x => x.From <= to && (!x.To.HasValue || x.To.Value >= from)) // overlap
            .OrderBy(x => x.PersonId)
            .ThenBy(x => x.From)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<TimesheetEntry?> GetActiveEntryOnDateAsync(
        Guid timelineId,
        Guid personId,
        DateOnly date,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        return await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimelineId == timelineId)
            .Where(x => x.PersonId == personId)
            .Where(x => x.From <= date && (!x.To.HasValue || x.To.Value >= date))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task AddAsync(TimesheetEntry entry, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var tl = await LoadTimelineForInsertAsync(db, entry.TimelineId, ct);

        EnsureEntryWithinTimeline(tl, entry);

        db.TimesheetEntries.Add(entry);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(TimesheetEntry entry, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var tl = await db.TimesheetTimelines
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == entry.TimelineId, ct)
            ?? throw new InvalidOperationException("Таймлайн не знайдено.");

        // update дозволяємо, але не дозволяємо зламати межі таймлайну
        EnsureEntryWithinTimeline(tl, entry);

        db.TimesheetEntries.Update(entry);
        await db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(Guid entryId, string reason, string author, DateTime nowUtc, CancellationToken ct = default)
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
    public async Task SaveTransitionAsync(TimesheetEntry prevUpdated, TimesheetEntry nextAdded, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var tl = await LoadTimelineForInsertAsync(db, prevUpdated.TimelineId, ct);

        if (nextAdded.TimelineId != prevUpdated.TimelineId)
            throw new InvalidOperationException("Перехід повинен виконуватись в межах одного таймлайну.");

        EnsureEntryWithinTimeline(tl, prevUpdated);
        EnsureEntryWithinTimeline(tl, nextAdded);

        db.TimesheetEntries.Update(prevUpdated);
        db.TimesheetEntries.Add(nextAdded);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <summary>
    /// Завантажує таймлайн для write-вставки/переходу та застосовує правило p.1:
    /// якщо таймлайн закритий — вставки/переходи заборонені.
    /// </summary>
    private static async Task<TimesheetTimeline> LoadTimelineForInsertAsync(
        AppDbContext db,
        Guid timelineId,
        CancellationToken ct)
    {
        var tl = await db.TimesheetTimelines
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == timelineId, ct)
            ?? throw new InvalidOperationException("Таймлайн не знайдено (дані пошкоджені).");

        if (tl.ClosedAt.HasValue)
            throw new InvalidOperationException(
                $"Таймлайн закритий {tl.ClosedAt.Value:yyyy-MM-dd}. Вставки/переходи заборонені.");

        return tl;
    }

    private static void EnsureEntryWithinTimeline(TimesheetTimeline tl, TimesheetEntry e)
    {
        if (e.TimelineId != tl.Id)
            throw new InvalidOperationException("TimesheetEntry.TimelineId не відповідає таймлайну.");

        if (e.PersonId != tl.PersonId)
            throw new InvalidOperationException("TimesheetEntry.PersonId не відповідає власнику таймлайну.");

        if (e.From < tl.OpenedAt)
            throw new InvalidOperationException(
                $"Запис не може починатися раніше OpenedAt ({tl.OpenedAt:yyyy-MM-dd}).");

        if (e.To.HasValue && e.To.Value < e.From)
            throw new InvalidOperationException("Некоректний період: To не може бути раніше From.");

        // p.2: якщо таймлайн колись буде закритий — тут залишаємо перевірку меж закриття.
        // (Зараз вставки в закритий таймлайн заборонені, але update (підрізання) може виконуватись в інших сценаріях.)
        if (tl.ClosedAt.HasValue)
        {
            if (!e.To.HasValue)
                throw new InvalidOperationException(
                    $"Open-ended запис заборонений у закритому таймлайні (ClosedAt={tl.ClosedAt:yyyy-MM-dd}).");

            if (e.To.Value > tl.ClosedAt.Value)
                throw new InvalidOperationException(
                    $"Запис виходить за межі закритого таймлайну (ClosedAt={tl.ClosedAt:yyyy-MM-dd}).");
        }
    }
}
