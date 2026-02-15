//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetLifecycleRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Write-repo для життєвого циклу табеля (епізоди).
///
/// Ключова ідея:
/// - Один епізод "в табелі" = один <see cref="TimeSheetAggregate"/> (OpenedAt..ClosedAt).
/// - На кожне нове зарахування (після виключення) створюється НОВИЙ таймлайн.
/// - Старі таймлайни не перезаписуються і не "перевідкриваються".
///
/// Правила:
/// - "НБ" не записуємо як entry — це derived стан (немає активного entry на дату).
/// - При Enroll:
///   - якщо активного епізоду немає — створюємо новий таймлайн (OpenedAt=enrollDate),
///     додаємо дефолтний код "Т" (open-ended) на enrollЕЮ дату (ідемпотентно).
///   - якщо активний епізод є — НЕ змінюємо OpenedAt і НЕ створюємо новий епізод (ідемпотентність для повторів).
/// - При Exclude:
///   - дозволяємо закривати табель лише з певних кодів на дату (наприклад "Т"/"РОЗПОР").
///   - закриваємо РІВНО один активний таймлайн (inclusive),
///   - clamping відкритих/довгих entry до closeTo,
///   - soft-delete future entries (From > closeTo).
/// </summary>
public sealed class TimesheetLifecycleRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetLifecycleRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <summary>Дефолтний код на дату зарахування (перший факт у новому епізоді).</summary>
    private const string DefaultEnrollCode = TimesheetSystemCodes.BaseState;

    /// <summary>Коди, з яких дозволено закривати табель при Exclude.</summary>
    private static readonly HashSet<string> AllowedCloseCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        TimesheetSystemCodes.BaseState,
        TimesheetSystemCodes.Rozpor
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

        var by = author.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1) Active episode (ClosedAt == null) — очікуємо максимум 1
        var active = await db.TimeSheets
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        TimeSheetAggregate timeline;

        if (active.Count == 1)
        {
            timeline = active[0];

            // Ідемпотентний повтор: нічого не "перезаписуємо"
            if (timeline.OpenedAt > enrollDate)
                throw new InvalidOperationException(
                    $"Неможливо відкрити табель на {enrollDate:yyyy-MM-dd}: активний епізод відкритий пізніше ({timeline.OpenedAt:yyyy-MM-dd}).");
        }
        else if (active.Count == 0)
        {
            // 2) Заборона відкривати НОВИЙ епізод "у минулому", який накладається на вже закриті епізоди.
            // Епізоди мають бути часово послідовні: enrollDate > lastClosedAt (якщо існує).
            var lastClosed = await db.TimeSheets
                .AsNoTracking()
                .Where(x => x.PersonId == personId && x.ClosedAt != null)
                .OrderByDescending(x => x.ClosedAt)
                .Select(x => x.ClosedAt)
                .FirstOrDefaultAsync(ct);

            if (lastClosed.HasValue && enrollDate <= lastClosed.Value)
            {
                throw new InvalidOperationException(
                    $"Неможливо відкрити новий табель на {enrollDate:yyyy-MM-dd}: " +
                    $"вже існує закритий табель до {lastClosed.Value:yyyy-MM-dd}. " +
                    "Табелі не можна накладати або відкривати 'заднім числом'.");
            }

            timeline = new TimeSheetAggregate
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                OpenedAt = enrollDate,
                ClosedAt = null,
                CreatedBy = by,
                CreatedAtUtc = nowUtc
            };

            db.TimeSheets.Add(timeline);
        }
        else
        {
            throw new InvalidOperationException(
                "Неможливо відкрити табель: знайдено декілька активних епізодів (дані пошкоджені).");
        }

        var defaultCodeId = await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Where(x => x.Code == DefaultEnrollCode)
            .Select(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (defaultCodeId == Guid.Empty)
            throw new InvalidOperationException($"Код '{DefaultEnrollCode}' не знайдено у довіднику TimesheetCodes.");

        // 3) Default code covering enroll date (ідемпотентно)
        var hasEntryOnEnrollDate = await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimesheetId == timeline.Id)
            .Where(x => x.From <= enrollDate && (!x.To.HasValue || x.To.Value >= enrollDate))
            .AnyAsync(ct);

        if (!hasEntryOnEnrollDate)
        {
            db.TimesheetEntries.Add(new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                TimesheetId = timeline.Id,
                PersonId = personId,
                TimesheetCodeDefinitionId = defaultCodeId,
                From = enrollDate,
                To = null,
                Reference = "Auto: enroll",
                Note = null,
                CreatedBy = by,
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

        var by = author.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1) Validate predecessor state (allowed code on closeTo)
        await ValidateCanCloseOnExcludeCoreAsync(db, personId, closeTo, ct);

        // 2) Load EXACTLY one active episode (не "закриваємо все підряд")
        var active = await db.TimeSheets
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        if (active.Count == 0)
            throw new InvalidOperationException("Неможливо виключити з табелю: немає активного епізоду.");

        if (active.Count > 1)
            throw new InvalidOperationException("Неможливо виключити з табелю: декілька активних епізодів (дані пошкоджені).");

        var tl = active[0];

        if (closeTo < tl.OpenedAt)
            throw new InvalidOperationException(
                $"Неможливо закрити табель на {closeTo:yyyy-MM-dd}: він відкритий з {tl.OpenedAt:yyyy-MM-dd}.");

        // 3) Close timeline (inclusive)
        tl.ClosedAt = closeTo;
        tl.ClosedBy = by;
        tl.ClosedAtUtc = nowUtc;

        // 4) Clamp entries that extend beyond closeTo (or are open-ended)
        var toClamp = await db.TimesheetEntries
            .Where(x => x.TimesheetId == tl.Id && !x.IsDeleted)
            .Where(x => x.From <= closeTo)
            .Where(x => x.To == null || x.To > closeTo)
            .ToListAsync(ct);

        foreach (var e in toClamp)
        {
            e.To = closeTo;
            e.UpdatedBy = by;
            e.UpdatedAtUtc = nowUtc;
        }

        // 5) Soft-delete future entries (From > closeTo)
        var future = await db.TimesheetEntries
            .Where(x => x.TimesheetId == tl.Id && !x.IsDeleted)
            .Where(x => x.From > closeTo)
            .ToListAsync(ct);

        if (future.Count > 0)
        {
            var msg = BuildExcludeDeleteReason(reason);
            foreach (var e in future)
            {
                e.IsDeleted = true;
                e.DeletedBy = by;
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
        var active = await db.TimeSheets
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .OrderByDescending(x => x.OpenedAt)
            .ThenByDescending(x => x.Id)
            .ToListAsync(ct);

        if (active.Count == 0)
            throw new InvalidOperationException("Неможливо виключити з табелю: немає активного епізоду.");

        if (active.Count > 1)
            throw new InvalidOperationException("Неможливо виключити з табелю: декілька активних епізодів (дані пошкоджені).");

        var timeline = active[0];

        var onDate = await db.TimesheetEntries
            .AsNoTracking()
            .Include(x => x.TimesheetCodeDefinition)
            .Where(x => x.TimesheetId == timeline.Id && !x.IsDeleted)
            .Where(x => x.From <= closeTo && (!x.To.HasValue || x.To.Value >= closeTo))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"Неможливо виключити з табелю: на дату {closeTo:yyyy-MM-dd} немає активного запису (дані пошкоджені).");

        if (onDate.TimesheetCodeDefinition is null)
            throw new InvalidOperationException("Неможливо перевірити код: TimesheetCodeDefinition не підвантажено/відсутнє.");

        var code = (onDate.TimesheetCodeDefinition.Code ?? string.Empty).Trim();

        if (!AllowedCloseCodes.Contains(code))
        {
            throw new InvalidOperationException(
                $"Неможливо виключити з табелю зі стану '{code}'. Дозволено тільки з 'Т' або 'РОЗПОР'. " +
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
