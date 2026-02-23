//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEpisodeRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Репозиторій епізодів табеля (<see cref="TimeSheetAggregate"/>): lifecycle + read-доступ.
///
/// <para>Ключова ідея:</para>
/// <list type="bullet">
/// <item><description>Один епізод "в табелі" = один <see cref="TimeSheetAggregate"/> (<c>OpenedAt..ClosedAt</c>).</description></item>
/// <item><description>На кожне нове зарахування (після виключення) створюється НОВИЙ епізод.</description></item>
/// <item><description>Епізоди не "перевідкриваються".</description></item>
/// </list>
/// </summary>
public sealed class TimesheetEpisodeRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetEpisodeRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <summary>Дефолтний код на дату зарахування (перший факт у новому епізоді).</summary>
    private const string DefaultEnrollCode = TimesheetSystemCodes.BaseState;

    /// <summary>Коди, з яких дозволено закривати табель при Exclude.</summary>
    private static readonly HashSet<string> AllowedCloseCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        TimesheetSystemCodes.BaseState,
        TimesheetSystemCodes.Rozpor,
        TimesheetSystemCodes.ReadyToCombatTask
    };

    //======================================================================
    // Read
    //======================================================================

    /// <inheritdoc />
    public async Task<TimeSheetAggregate?> GetEpisodeOnDateAsync(Guid personId, DateOnly date, CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("personId is required.", nameof(personId));
        if (date == default) throw new ArgumentException("date is required.", nameof(date));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Expect at most one episode that covers the date.
        return await db.TimeSheets
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.PersonId == personId
                && x.OpenedAt <= date
                && (!x.ClosedAt.HasValue || x.ClosedAt.Value >= date), ct);
    }

    /// <inheritdoc />
    public async Task<TimeSheetAggregate?> GetActiveEpisodeAsync(Guid personId, CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("personId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // Active episode is unique by index (ClosedAt == null).
        return await db.TimeSheets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.PersonId == personId && x.ClosedAt == null, ct);
    }

    //======================================================================
    // Lifecycle
    //======================================================================

    /// <inheritdoc />
    public async Task OpenOnEnrollAsync(
        Guid personId,
        DateOnly enrollDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        EnsureAuthor(author);
        EnsureUtcNow(nowUtc);
        if (personId == Guid.Empty)
            throw new ArgumentException("personId is required.", nameof(personId));
        if (enrollDate == default)
            throw new ArgumentException("enrollDate is required.", nameof(enrollDate));

        var by = author.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1) Active episode (ClosedAt == null) — очікуємо максимум 1.
        var active = await db.TimeSheets
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .ToListAsync(ct);

        TimeSheetAggregate timeline;

        if (active.Count == 1)
        {
            timeline = active[0];

            // Ідемпотентний повтор: нічого не "перезаписуємо".
            if (timeline.OpenedAt > enrollDate)
                throw new InvalidOperationException(
                    $"Неможливо відкрити табель на {enrollDate:yyyy-MM-dd}: активний епізод відкритий пізніше ({timeline.OpenedAt:yyyy-MM-dd}).");
        }
        else if (active.Count == 0)
        {
            // 2) Заборона відкривати НОВИЙ епізод "у минулому", який накладається на вже закриті епізоди.
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
            .SingleOrDefaultAsync(ct);

        if (defaultCodeId == Guid.Empty)
            throw new InvalidOperationException($"Код '{DefaultEnrollCode}' не знайдено у довіднику TimesheetCodes.");

        // 3) Default code covering enroll date (ідемпотентно)
        var hasEntryOnEnrollDate = await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimesheetId == timeline.Id)
            .Where(x => x.From <= enrollDate && (!x.To.HasValue || enrollDate < x.To.Value))
            .AnyAsync(ct);

        if (!hasEntryOnEnrollDate)
        {
            // Усі події — тільки через агрегат.
            timeline.AddTimesheetEntry(
                nextCodeId: defaultCodeId,
                effectiveAt: enrollDate,
                reference: "Auto: enroll",
                author: by,
                nowUtc: nowUtc);
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    /// <inheritdoc />
    public async Task ValidateCanCloseOnExcludeAsync(Guid personId, DateOnly closeTo, CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("personId is required.", nameof(personId));
        if (closeTo == default)
            throw new ArgumentException("closeTo is required.", nameof(closeTo));

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
        EnsureUtcNow(nowUtc);
        if (personId == Guid.Empty)
            throw new ArgumentException("personId is required.", nameof(personId));
        if (closeTo == default)
            throw new ArgumentException("closeTo is required.", nameof(closeTo));

        var by = author.Trim();

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // 1) Validate predecessor state (allowed code on closeTo)
        await ValidateCanCloseOnExcludeCoreAsync(db, personId, closeTo, ct);

        // 2) Load exactly one active episode (tracked + entries)
        var active = await db.TimeSheets
            .Include(t => t.Entries)
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .ToListAsync(ct);

        if (active.Count == 0)
            throw new InvalidOperationException("Неможливо виключити з табелю: немає активного епізоду.");

        if (active.Count > 1)
            throw new InvalidOperationException("Неможливо виключити з табелю: декілька активних епізодів (дані пошкоджені).");

        var tl = active[0];

        // 3) Close episode (inclusive) through aggregate.
        // IMPORTANT: CloseEpisode uses provided reason as DeleteReason for future entries.
        // On Exclude we store a stable system message for audit/debug.
        var closeExclusive = closeTo.AddDays(1);
        var deleteReason = BuildExcludeDeleteReason(reason);

        tl.CloseEpisode(
            closedAtInclusive: closeTo,
            reason: deleteReason,
            author: by,
            nowUtc: nowUtc);

        // 4) Clamp audit: closing modifies the last active entry To -> ClosedAt+1.
        // We touch audit explicitly to avoid "silent" timeline changes.
        var lastActive = tl.Entries
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.From)
            .FirstOrDefault();

        if (lastActive is not null && lastActive.To == closeExclusive)
        {
            lastActive.UpdatedBy = by;
            lastActive.UpdatedAtUtc = nowUtc;
        }

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    //======================================================================
    // Internals
    //======================================================================

    private static async Task ValidateCanCloseOnExcludeCoreAsync(
        AppDbContext db,
        Guid personId,
        DateOnly closeTo,
        CancellationToken ct)
    {
        var active = await db.TimeSheets
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .ToListAsync(ct);

        if (active.Count == 0)
            throw new InvalidOperationException("Неможливо виключити з табелю: немає активного епізоду.");

        if (active.Count > 1)
            throw new InvalidOperationException("Неможливо виключити з табелю: декілька активних епізодів (дані пошкоджені).");

        var timeline = active[0];

        // 0) Неможливо закривати раніше відкриття епізоду
        if (closeTo < timeline.OpenedAt)
        {
            throw new InvalidOperationException(
                $"Неможливо закрити табель на {closeTo:yyyy-MM-dd}: він відкритий з {timeline.OpenedAt:yyyy-MM-dd}.");
        }

        // 1) На дату closeTo має існувати активний запис
        var onDate = await db.TimesheetEntries
            .AsNoTracking()
            .Include(x => x.TimesheetCodeDefinition)
            .Where(x => x.TimesheetId == timeline.Id && !x.IsDeleted)
            .Where(x => x.From <= closeTo && (!x.To.HasValue || closeTo < x.To.Value))
            .OrderByDescending(x => x.From)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException(
                $"Неможливо виключити з табелю: на дату {closeTo:yyyy-MM-dd} немає активного запису (дані пошкоджені).");

        if (onDate.TimesheetCodeDefinition is null)
            throw new InvalidOperationException("Неможливо перевірити код: TimesheetCodeDefinition не підвантажено/відсутнє.");

        var code = (onDate.TimesheetCodeDefinition.Code ?? string.Empty).Trim();

        if (!AllowedCloseCodes.Contains(code))
        {
            throw new InvalidOperationException(
                $"Неможливо виключити з табелю зі стану '{code}'. " +
                $"Дозволено тільки з '{TimesheetSystemCodes.BaseState}', '{TimesheetSystemCodes.Rozpor}' або '{TimesheetSystemCodes.ReadyToCombatTask}'. " +
                $"Спочатку приведіть табель до дозволеного стану на {closeTo:yyyy-MM-dd}.");
        }
    }

    private static void EnsureAuthor(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("author is required.", nameof(author));
    }

    private static void EnsureUtcNow(DateTime nowUtc)
    {
        if (nowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("nowUtc must be UTC.", nameof(nowUtc));
    }

    private static string BuildExcludeDeleteReason(string? reason)
    {
        var r = (reason ?? string.Empty).Trim();

        return string.IsNullOrWhiteSpace(r)
            ? "Auto-deleted: person excluded"
            : $"Auto-deleted: person excluded ({r})";
    }
}
