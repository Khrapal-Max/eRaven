//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusReadService
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PersonStatusReadService;

public sealed class PersonStatusReadService(IDbContextFactory<AppDbContext> dbf) : IPersonStatusReadService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;
    private static readonly IComparer<StatusKind> StatusKindPriorityComparer = Comparer<StatusKind>.Create(StatusPriorityComparer.Compare);
    private const string NotPresentCode = "нб";

    // ====================== ПУБЛІЧНІ АПІ ======================

    public async Task<PersonStatus?> GetActiveOnDateAsync(Guid personId, DateTime endOfDayUtc, CancellationToken ct = default)
    {
        if (personId == Guid.Empty) return null;

        var momentUtc = NormalizeUtc(endOfDayUtc);

        await using var db = await _dbf.CreateDbContextAsync(ct);

        var firstPresenceMap = await BuildFirstPresenceMapAsync(db, [personId], momentUtc, ct);
        var firstPresenceUtc = firstPresenceMap.TryGetValue(personId, out var fp) ? fp : null;
        var slice = await StatusPriorityComparer
            .OrderForPointInTime(db.PersonStatuses.AsNoTracking()
                .Include(s => s.StatusKind)
                .Where(s => s.IsActive && s.PersonId == personId && s.OpenDate <= momentUtc))
            .ThenBy(s => s.Sequence)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

        var timeline = SelectTimeline(slice);
        var chosen = timeline.LastOrDefault(s => s.OpenDate <= momentUtc);
        if (chosen is not null)
            return chosen;

        if (firstPresenceUtc is not null && momentUtc < firstPresenceUtc.Value)
        {
            var notPresent = await FindStatusKindByCodeAsync(db, NotPresentCode, ct);
            if (notPresent is null)
                return null;

            return new PersonStatus
            {
                Id = Guid.Empty,
                PersonId = personId,
                StatusKindId = notPresent.Id,
                StatusKind = notPresent,
                OpenDate = momentUtc,
                IsActive = true,
                Sequence = 0,
                Author = null,
                Modified = momentUtc
            };
        }

        return null;
    }

    public async Task<PersonStatus?> ResolveOnDateAsync(Guid personId, DateTime dayUtc, CancellationToken ct = default)
    {
        var dict = await ResolveOnDateAsync([personId], dayUtc, ct);
        return dict.TryGetValue(personId, out var s) ? s : null;
    }

    public async Task<IReadOnlyDictionary<Guid, PersonStatus?>> ResolveOnDateAsync(
        IEnumerable<Guid> personIds, DateTime dayUtc, CancellationToken ct = default)
    {
        var ids = personIds?.Distinct().ToArray() ?? [];
        if (ids.Length == 0) return new Dictionary<Guid, PersonStatus?>();

        await using var db = await _dbf.CreateDbContextAsync(ct);

        // Межі локального дня для переданого UTC-моменту
        var (_, dayEndExclusiveUtc) = GetLocalDayBoundsUtc(dayUtc);
        var endOfDayUtc = dayEndExclusiveUtc.AddTicks(-1);

        var slice = await StatusPriorityComparer
            .OrderForPointInTime(db.PersonStatuses.AsNoTracking()
                .Include(s => s.StatusKind)
                .Where(s => s.IsActive && ids.Contains(s.PersonId) && s.OpenDate <= endOfDayUtc))
            .ThenBy(s => s.Sequence)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

        var firstPresenceMap = await BuildFirstPresenceMapAsync(db, ids, endOfDayUtc, ct);
        var byPerson = slice.GroupBy(s => s.PersonId)
            .ToDictionary(g => g.Key, g => SelectTimeline([.. g]));

        var result = new Dictionary<Guid, PersonStatus?>(ids.Length);

        foreach (var pid in ids)
        {
            var firstPresenceUtc = firstPresenceMap.TryGetValue(pid, out var fp) ? fp : null;
            if (firstPresenceUtc is null || endOfDayUtc < firstPresenceUtc.Value)
            {
                result[pid] = null;
                continue;
            }

            if (!byPerson.TryGetValue(pid, out var list) || list.Count == 0)
            {
                result[pid] = null;
                continue;
            }
            var chosen = list.LastOrDefault(s => s.OpenDate <= endOfDayUtc);
            result[pid] = chosen;
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, PersonMonthStatus>> ResolveMonthAsync(
        IEnumerable<Guid> personIds, int yearLocal, int monthLocal, CancellationToken ct = default)
    {
        var ids = personIds?.Distinct().ToArray() ?? [];
        if (ids.Length == 0) return new Dictionary<Guid, PersonMonthStatus>();

        var daysInMonth = DateTime.DaysInMonth(yearLocal, monthLocal);
        var monthStartLocal = new DateTime(yearLocal, monthLocal, 1);
        var monthEndLocal = monthStartLocal.AddMonths(1);

        // Готуємо межі у UTC на кожен день місяця
        var bounds = new (DateTime startUtc, DateTime endUtc)[daysInMonth];
        for (int i = 0; i < daysInMonth; i++)
        {
            var localDay = monthStartLocal.AddDays(i);
            bounds[i] = GetLocalDayBoundsUtc(localDay);
        }

        await using var db = await _dbf.CreateDbContextAsync(ct);
        // Беремо усі статуси за місяць + “хвіст” до першого дня для baseline
        var monthEndUtc = bounds[^1].endUtc;

        var slice = await StatusPriorityComparer
            .OrderForHistory(db.PersonStatuses.AsNoTracking()
                .Include(s => s.StatusKind)
                .Where(s => s.IsActive && ids.Contains(s.PersonId) && s.OpenDate <= monthEndUtc))
            .ThenBy(s => s.Sequence)
            .ToListAsync(ct);

        // Перші призначення
        var firstPresenceMap = await BuildFirstPresenceMapAsync(db, ids, monthEndUtc, ct);

        var byPerson = slice.GroupBy(s => s.PersonId)
            .ToDictionary(g => g.Key, g => SelectTimeline(g.ToList()));

        var map = new Dictionary<Guid, PersonMonthStatus>(ids.Length);

        foreach (var pid in ids)
        {
            var row = new PersonStatus?[daysInMonth];
            var timeline = byPerson.TryGetValue(pid, out var list) ? list : [];
            var firstPresenceUtc = firstPresenceMap.TryGetValue(pid, out var fp) ? fp : null;

            var cursor = 0;
            PersonStatus? current = null;

            for (int di = 0; di < daysInMonth; di++)
            {
                var (_, dayEndExclusiveUtc) = bounds[di];
                var endOfDayUtc = dayEndExclusiveUtc.AddTicks(-1);

                while (cursor < timeline.Count && timeline[cursor].OpenDate <= endOfDayUtc)
                {
                    current = timeline[cursor];
                    cursor++;
                }

                if (firstPresenceUtc is null || endOfDayUtc < firstPresenceUtc.Value)
                {
                    row[di] = null;
                    continue;
                }

                row[di] = current;
            }

            map[pid] = new PersonMonthStatus(row, firstPresenceUtc);
        }

        return ordered.AsReadOnly();
    }

    public async Task<DateTime?> GetFirstPresenceUtcAsync(Guid personId, CancellationToken ct = default)
    {
        if (personId == Guid.Empty) return null;

        await using var db = await _dbf.CreateDbContextAsync(ct);
        var map = await BuildFirstPresenceMapAsync(db, [personId], DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), ct);
        return map.TryGetValue(personId, out var value) ? value : null;
    }

    public async Task<StatusKind?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        await using var db = await _dbf.CreateDbContextAsync(ct);
        return await FindStatusKindByCodeAsync(db, code, ct);
    }

    public async Task<IReadOnlyList<PersonStatus>> OrderForHistoryAsync(Guid personId, CancellationToken ct = default)
    {
        if (personId == Guid.Empty) return [];

        await using var db = await _dbf.CreateDbContextAsync(ct);

        var ordered = await StatusPriorityComparer
            .OrderForHistory(db.PersonStatuses.AsNoTracking()
                .Include(s => s.StatusKind)
                .Where(s => s.PersonId == personId && s.IsActive))
            .ToListAsync(ct);

        return ordered.AsReadOnly();
    }

    public async Task<DateTime?> GetFirstPresenceUtcAsync(Guid personId, CancellationToken ct = default)
    {
        if (personId == Guid.Empty) return null;

        await using var db = await _dbf.CreateDbContextAsync(ct);
        var map = await BuildFirstPresenceMapAsync(db, [personId], DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), ct);
        return map.TryGetValue(personId, out var value) ? value : null;
    }

    public async Task<StatusKind?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        await using var db = await _dbf.CreateDbContextAsync(ct);
        return await FindStatusKindByCodeAsync(db, code, ct);
    }
    
    public Task<StatusKind?> ResolveNotPresentAsync(CancellationToken ct = default)
        => GetByCodeAsync(NotPresentCode, ct);

    // ====================== ДОПОМОЖНІ ======================

    // Єдиний спосіб обчислити межі «локального дня» для поданого UTC/Local моменту.
    // Проєкт однокоробковий → використовуємо системну локаль:
    private static (DateTime startUtc, DateTime endUtc) GetLocalDayBoundsUtc(DateTime anyUtcOrLocal)
    {
        // Нормалізуємо до UTC → у локальний → беремо календарний день → назад у UTC.
        var asUtc = anyUtcOrLocal.Kind switch
        {
            DateTimeKind.Utc => anyUtcOrLocal,
            DateTimeKind.Local => anyUtcOrLocal.ToUniversalTime(),
            _ => DateTime.SpecifyKind(anyUtcOrLocal, DateTimeKind.Utc)
        };

        var local = asUtc.ToLocalTime().Date; // 00:00 локального дня
        var startUtc = DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
        var endUtc = DateTime.SpecifyKind(local.AddDays(1), DateTimeKind.Local).ToUniversalTime();
        return (startUtc, endUtc);
    }

    private static DateTime? MinDate(DateTime? a, DateTime? b)
    {
        if (a is null) return b;
        if (b is null) return a;
        return a <= b ? a : b;
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }

    private static List<PersonStatus> SelectTimeline(IReadOnlyCollection<PersonStatus> statuses)
    {
        if (statuses.Count == 0) return [];

        return [.. statuses
            .GroupBy(s => s.OpenDate)
            .OrderBy(g => g.Key)
            .Select(g => g
                .OrderBy(s => s.StatusKind!, StatusKindPriorityComparer)
                .ThenBy(s => s.Sequence)
                .ThenBy(s => s.Id)
                .First())];
    }

    private async Task<Dictionary<Guid, DateTime?>> BuildFirstPresenceMapAsync(
        AppDbContext db,
        IReadOnlyCollection<Guid> personIds,
        DateTime untilUtc,
        CancellationToken ct)
    {
        var map = personIds.ToDictionary(id => id, _ => (DateTime?)null);

        var kinds = await db.StatusKinds.AsNoTracking().ToListAsync(ct);
        var inDistrict = kinds.FirstOrDefault(k => string.Equals(k.Name?.Trim(), "В районі", StringComparison.OrdinalIgnoreCase));

        if (inDistrict is not null)
        {
            var statuses = await db.PersonStatuses.AsNoTracking()
                .Where(s => s.IsActive && personIds.Contains(s.PersonId) && s.StatusKindId == inDistrict.Id && s.OpenDate <= untilUtc)
                .GroupBy(s => s.PersonId)
                .Select(g => new { g.Key, FirstUtc = g.Min(x => x.OpenDate) })
                .ToListAsync(ct);

            foreach (var item in statuses)
            {
                if (map.TryGetValue(item.Key, out var current))
                    map[item.Key] = MinDate(current, item.FirstUtc);
            }
        }

        if (db.Model.FindEntityType(typeof(PersonPositionAssignment)) is not null)
        {
            var assignments = await db.PersonPositionAssignments.AsNoTracking()
                .Where(a => personIds.Contains(a.PersonId) && a.OpenUtc <= untilUtc)
                .GroupBy(a => a.PersonId)
                .Select(g => new { g.Key, FirstUtc = g.Min(x => x.OpenUtc) })
                .ToListAsync(ct);

            foreach (var item in assignments)
            {
                if (map.TryGetValue(item.Key, out var current))
                    map[item.Key] = MinDate(current, item.FirstUtc);
            }
        }

        return map;
    }

    private static async Task<StatusKind?> FindStatusKindByCodeAsync(AppDbContext db, string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalizedUpper = code.Trim().ToUpperInvariant();

        return await db.StatusKinds.AsNoTracking()
            .FirstOrDefaultAsync(k => k.Code != null && k.Code.Equals(normalizedUpper, StringComparison.InvariantCultureIgnoreCase), ct);
    }
}
