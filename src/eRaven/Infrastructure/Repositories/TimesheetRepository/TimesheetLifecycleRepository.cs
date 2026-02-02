//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetLifecycleRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Write-repo для життєвого циклу табеля (facts-only).
///
/// Правила:
/// - "НБ" не записуємо як entry — це derived стан (немає активного entry на дату).
/// - При Enroll: відкриваємо timeline (якщо немає активного) і забезпечуємо MAIN-код "30" на дату.
/// - При Exclude: дозволяємо закривати табель тільки з певних кодів на дату (наприклад 30/РОЗПОР),
///   далі:
///   - закриваємо timeline (inclusive)
///   - clamping відкритих/довгих entry до closeTo
///   - soft-delete future entries (From > closeTo)
/// </summary>
public sealed class TimesheetLifecycleRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetLifecycleRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <summary>
    /// Коди, з яких дозволено закривати табель при Exclude.
    /// </summary>
    private static readonly HashSet<string> AllowedCloseCodes = new(StringComparer.Ordinal)
    {
        "30",
        "РОЗПОР"
    };

    /// <inheritdoc />
    public async Task OpenOnEnrollAsync(
        Guid personId,
        DateOnly enrollDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Active timeline (ClosedAt == null). Expect максимум 1.
        var active = await db.TimesheetTimelines
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        TimesheetTimeline timeline;

        if (active.Count == 0)
        {
            timeline = new TimesheetTimeline
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                OpenedAt = enrollDate,
                ClosedAt = null,
                CreatedBy = author.Trim(),
                CreatedAtUtc = nowUtc
            };

            db.TimesheetTimelines.Add(timeline);
        }
        else if (active.Count == 1)
        {
            timeline = active[0];

            // Enroll не може бути раніше відкриття активної шкали (це означає неконсистентність даних).
            if (timeline.OpenedAt > enrollDate)
                throw new InvalidOperationException(
                    $"Неможливо відкрити табель на {enrollDate:yyyy-MM-dd}: активна шкала відкрита пізніше ({timeline.OpenedAt:yyyy-MM-dd}).");
        }
        else
        {
            throw new InvalidOperationException(
                "Неможливо відкрити табель: знайдено декілька активних шкал (дані пошкоджені).");
        }

        // Default "30" covering enroll date (idempotent)
        var hasEntryOnEnrollDate = await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimelineId == timeline.Id)
            .Where(x => x.From <= enrollDate && (!x.To.HasValue || x.To.Value >= enrollDate))
            .AnyAsync(ct);

        if (!hasEntryOnEnrollDate)
        {
            db.TimesheetEntries.Add(new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                TimelineId = timeline.Id,
                PersonId = personId,
                Code = "30",
                From = enrollDate,
                To = null,
                Reference = "Auto: Enroll",
                Note = null,
                CreatedBy = author.Trim(),
                CreatedAtUtc = nowUtc,
                IsDeleted = false
            });
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task ValidateCanCloseOnExcludeAsync(Guid personId, DateOnly closeTo, CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await ValidateCanCloseOnExcludeCoreAsync(db, personId, closeTo, ct);
    }

    /// <inheritdoc />
    public async Task CloseOnExcludeAsync(
        Guid personId,
        DateOnly closeTo,
        string? reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Validate predecessor state (code must be 30/РОЗПОР on closeTo)
        await ValidateCanCloseOnExcludeCoreAsync(db, personId, closeTo, ct);

        var active = await db.TimesheetTimelines
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .ToListAsync(ct);

        if (active.Count == 0)
            throw new InvalidOperationException(
                "Неможливо виключити з табелю: немає активної шкали (особа вже поза табелем).");

        var activeTimelineIds = active.Select(x => x.Id).ToArray();

        // 1) Close timelines (inclusive)
        foreach (var tl in active)
        {
            tl.ClosedAt = closeTo;
            tl.ClosedBy = author.Trim();
            tl.ClosedAtUtc = nowUtc;
        }

        // 2) Clamp entries that extend beyond closeTo (or are open-ended)
        var toClamp = await db.TimesheetEntries
            .Where(x => activeTimelineIds.Contains(x.TimelineId) && !x.IsDeleted)
            .Where(x => x.From <= closeTo)
            .Where(x => x.To == null || x.To > closeTo)
            .ToListAsync(ct);

        foreach (var e in toClamp)
        {
            e.To = closeTo;
            e.UpdatedBy = author.Trim();
            e.UpdatedAtUtc = nowUtc;
        }

        // 3) Soft-delete future entries (From > closeTo)
        var future = await db.TimesheetEntries
            .Where(x => activeTimelineIds.Contains(x.TimelineId) && !x.IsDeleted)
            .Where(x => x.From > closeTo)
            .ToListAsync(ct);

        if (future.Count > 0)
        {
            var msg = BuildExcludeDeleteReason(reason);
            foreach (var e in future)
            {
                e.IsDeleted = true;
                e.DeletedBy = author.Trim();
                e.DeletedAtUtc = nowUtc;
                e.DeleteReason = msg;
            }
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static async Task ValidateCanCloseOnExcludeCoreAsync(
        AppDbContext db,
        Guid personId,
        DateOnly closeTo,
        CancellationToken ct)
    {
        var active = await db.TimesheetTimelines
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        if (active.Count == 0)
            throw new InvalidOperationException(
                "Неможливо виключити з табелю: немає активної шкали (особа вже поза табелем).");

        if (active.Count > 1)
            throw new InvalidOperationException(
                "Неможливо виключити з табелю: знайдено декілька активних шкал (дані пошкоджені).");

        var timeline = active[0];

        var onDate = await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.TimelineId == timeline.Id && !x.IsDeleted)
            .Where(x => x.From <= closeTo && (!x.To.HasValue || x.To.Value >= closeTo))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct) ??
            throw new InvalidOperationException(
                $"Неможливо виключити з табелю: на дату {closeTo:yyyy-MM-dd} немає активного запису (дані пошкоджені).");

        var code = (onDate.Code ?? string.Empty).Trim();

        if (!AllowedCloseCodes.Contains(code))
        {
            throw new InvalidOperationException(
                $"Неможливо виключити з табелю зі стану '{code}'. Дозволено тільки з '30' або 'РОЗПОР'. " +
                $"Спочатку приведіть табель до дозволеного стану на {closeTo:yyyy-MM-dd}.");
        }
    }

    private static void EnsureAuthor(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required.", nameof(author));
    }

    private static string BuildExcludeDeleteReason(string? reason)
    {
        var r = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return r is null
            ? "Auto-deleted: person excluded"
            : $"Auto-deleted: person excluded ({r})";
    }
}
