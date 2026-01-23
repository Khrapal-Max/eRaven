//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//----------------------------------------------------------------------------- 
// TimesheetLifecycleRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public sealed class TimesheetLifecycleRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetLifecycleRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    // NB не записуємо як entry — він "derived" (немає активного Main entry).
    // Тому при Exclude дозволяємо закривати табель тільки з певних станів Main.
    private static readonly HashSet<string> AllowedCloseMainCodes = new(StringComparer.Ordinal)
    {
        "30",
        "РОЗПОР"
    };

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

        // Active timelines (ClosedAt == null)
        var active = await db.TimesheetTimelines
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .ToListAsync(ct);

        var main = active.SingleOrDefault(x => x.Lane == TimesheetLane.Main);
        var task = active.SingleOrDefault(x => x.Lane == TimesheetLane.Task);

        if (main is null)
        {
            main = new TimesheetTimeline
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Lane = TimesheetLane.Main,
                OpenedAt = enrollDate,
                ClosedAt = null,
                CreatedBy = author.Trim(),
                CreatedAtUtc = nowUtc
            };
            db.TimesheetTimelines.Add(main);
        }

        if (task is null)
        {
            task = new TimesheetTimeline
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Lane = TimesheetLane.Task,
                OpenedAt = enrollDate,
                ClosedAt = null,
                CreatedBy = author.Trim(),
                CreatedAtUtc = nowUtc
            };
            db.TimesheetTimelines.Add(task);
        }

        // Default MAIN "30" covering enroll date (idempotent)
        var hasMainOnEnrollDate = await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .Where(x => x.TimelineId == main.Id)
            .Where(x => x.Lane == TimesheetLane.Main)
            .Where(x => x.From <= enrollDate && (!x.To.HasValue || x.To.Value >= enrollDate))
            .AnyAsync(ct);

        if (!hasMainOnEnrollDate)
        {
            db.TimesheetEntries.Add(new TimesheetEntry
            {
                Id = Guid.NewGuid(),
                TimelineId = main.Id,
                PersonId = personId,
                Lane = TimesheetLane.Main,
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

    public async Task ValidateCanCloseOnExcludeAsync(Guid personId, DateOnly closeTo, CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(personId));

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var mainTimeline = await db.TimesheetTimelines
            .AsNoTracking()
            .Where(x => x.PersonId == personId && x.Lane == TimesheetLane.Main && x.ClosedAt == null)
            .SingleOrDefaultAsync(ct);

        if (mainTimeline is null)
            throw new InvalidOperationException(
                "Неможливо виключити з табелю: немає активної шкали Main (особа вже поза табелем).");

        var mainOnDate = await db.TimesheetEntries
            .AsNoTracking()
            .Where(x => x.TimelineId == mainTimeline.Id && x.Lane == TimesheetLane.Main && !x.IsDeleted)
            .Where(x => x.From <= closeTo && (!x.To.HasValue || x.To.Value >= closeTo))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (mainOnDate is null)
            throw new InvalidOperationException(
                $"Неможливо виключити з табелю: на дату {closeTo:yyyy-MM-dd} немає активного запису Main (дані пошкоджені).");

        var code = (mainOnDate.Code ?? string.Empty).Trim();
        if (!AllowedCloseMainCodes.Contains(code))
        {
            throw new InvalidOperationException(
                $"Неможливо виключити з табелю зі стану '{code}'. Дозволено тільки з '30' або 'РОЗПОР'. " +
                $"Спочатку приведіть Main до дозволеного стану на {closeTo:yyyy-MM-dd}.");
        }
    }

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

        // Validate predecessor state (Main code must be 30/РОЗПОР)
        await ValidateCanCloseOnExcludeCoreAsync(db, personId, closeTo, ct);

        var active = await db.TimesheetTimelines
            .Where(x => x.PersonId == personId && x.ClosedAt == null)
            .ToListAsync(ct);

        if (active.Count == 0)
            throw new InvalidOperationException("Неможливо виключити з табелю: немає активних шкал (особа вже поза табелем).");

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

    private static async Task ValidateCanCloseOnExcludeCoreAsync(AppDbContext db, Guid personId, DateOnly closeTo, CancellationToken ct)
    {
        var mainTimeline = await db.TimesheetTimelines
            .Where(x => x.PersonId == personId && x.Lane == TimesheetLane.Main && x.ClosedAt == null)
            .SingleOrDefaultAsync(ct);

        if (mainTimeline is null)
            throw new InvalidOperationException(
                "Неможливо виключити з табелю: немає активної шкали Main (особа вже поза табелем).");

        var mainOnDate = await db.TimesheetEntries
            .Where(x => x.TimelineId == mainTimeline.Id && x.Lane == TimesheetLane.Main && !x.IsDeleted)
            .Where(x => x.From <= closeTo && (!x.To.HasValue || x.To.Value >= closeTo))
            .OrderByDescending(x => x.From)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct);

        if (mainOnDate is null)
            throw new InvalidOperationException(
                $"Неможливо виключити з табелю: на дату {closeTo:yyyy-MM-dd} немає активного запису Main (дані пошкоджені).");

        var code = (mainOnDate.Code ?? string.Empty).Trim();
        if (!AllowedCloseMainCodes.Contains(code))
        {
            throw new InvalidOperationException(
                $"Неможливо виключити з табелю зі стану '{code}'. Дозволено тільки з '30' або 'РОЗПОР'. " +
                $"Спочатку приведіть Main до дозволеного стану на {closeTo:yyyy-MM-dd}.");
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
