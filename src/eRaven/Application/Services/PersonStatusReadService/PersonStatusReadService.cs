//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusReadService
//-----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PersonStatusReadService;

public sealed class PersonStatusReadService(IDbContextFactory<AppDbContext> dbf) : IPersonStatusReadService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;
    private static readonly IComparer<StatusKind> StatusKindPriorityComparer = Comparer<StatusKind>.Create(StatusPriorityComparer.Compare);

    // ====================== ПУБЛІЧНІ АПІ ======================

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
        var (dayStartUtc, dayEndUtc) = GetLocalDayBoundsUtc(dayUtc);

        // Довідник статусів (для пошуку "30" і "нб")
        var kinds = await db.StatusKinds.AsNoTracking().ToListAsync(ct);
        var kind30 = kinds.FirstOrDefault(k => string.Equals(k.Code, "30", StringComparison.OrdinalIgnoreCase));
        var kindNb = kinds.FirstOrDefault(k => string.Equals(k.Code, "нб", StringComparison.OrdinalIgnoreCase));

        // Усі статуси до кінця дня
        var slice = await StatusPriorityComparer
            .OrderForPointInTime(db.PersonStatuses.AsNoTracking()
                .Include(s => s.StatusKind)
                .Where(s => s.IsActive && ids.Contains(s.PersonId) && s.OpenDate < dayEndUtc))
            .ToListAsync(ct);

        // Перше призначення на посаду (якщо таблиця є)
        var firstAssign = await db.PersonPositionAssignments.AsNoTracking()
            .Where(a => ids.Contains(a.PersonId))
            .GroupBy(a => a.PersonId)
            .Select(g => new { PersonId = g.Key, FirstOpenUtc = g.Min(x => x.OpenUtc) })
            .ToListAsync(ct);
        var firstAssignMap = firstAssign.ToDictionary(x => x.PersonId, x => x.FirstOpenUtc);

        var result = new Dictionary<Guid, PersonStatus?>(ids.Length);

        foreach (var pid in ids)
        {
            var items = slice
                .Where(s => s.PersonId == pid)
                .OrderBy(s => s.OpenDate)
                .ThenBy(s => s.StatusKind!, StatusKindPriorityComparer)
                .ThenBy(s => s.Id)
                .ToList();

            // Перша поява (або через призначення, або перший статус)
            DateTime? firstPresenceUtc = items.Count == 0 ? null : items.Min(s => s.OpenDate);
            if (firstAssignMap.TryGetValue(pid, out var fa))
                firstPresenceUtc = firstPresenceUtc is null ? fa : (fa < firstPresenceUtc ? fa : firstPresenceUtc);

            // Якщо людини ще "не існує" на цей день → нб
            if (firstPresenceUtc is null || dayEndUtc <= firstPresenceUtc.Value)
            {
                result[pid] = kindNb is null ? null : Synthetic(kindNb, pid);
                continue;
            }

            // baseline < dayStart
            var baseline = items
                .Where(x => x.OpenDate < dayStartUtc)
                .OrderByDescending(x => x.OpenDate)
                .ThenByDescending(x => x.Sequence)
                .FirstOrDefault();

            // Якщо baseline нема (але поява вже була) — фон 30
            PersonStatus? synthetic = baseline is null && kind30 is not null ? Synthetic(kind30, pid) : null;

            // події в межах дня
            var inDay = items.Where(x => x.OpenDate >= dayStartUtc && x.OpenDate < dayEndUtc);

            var contenders = new List<PersonStatus>(8);
            if (baseline is not null) contenders.Add(baseline);
            if (synthetic is not null) contenders.Add(synthetic);
            contenders.AddRange(inDay);

            if (contenders.Count == 0)
            {
                // на випадок, якщо немає жодного запису (не повинно статись після гейтів вище)
                result[pid] = kind30 is null ? null : Synthetic(kind30, pid);
                continue;
            }

            var chosen = contenders
                .OrderBy(c => c.StatusKind!, StatusKindPriorityComparer)
                .ThenByDescending(c => c.OpenDate)
                .ThenByDescending(c => c.Sequence)
                .First();

            result[pid] = chosen;
        }

        return result;
    }

    public async Task<IReadOnlyDictionary<Guid, PersonStatus?[]>> ResolveMonthAsync(
        IEnumerable<Guid> personIds, int yearLocal, int monthLocal, CancellationToken ct = default)
    {
        var ids = personIds?.Distinct().ToArray() ?? [];
        if (ids.Length == 0) return new Dictionary<Guid, PersonStatus?[]>();

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

        // Довідник
        var kinds = await db.StatusKinds.AsNoTracking().ToListAsync(ct);
        var kind30 = kinds.FirstOrDefault(k => string.Equals(k.Code, "30", StringComparison.OrdinalIgnoreCase));
        var kindNb = kinds.FirstOrDefault(k => string.Equals(k.Code, "нб", StringComparison.OrdinalIgnoreCase));

        // Беремо усі статуси за місяць + “хвіст” до першого дня для baseline
        var monthStartUtc = bounds[0].startUtc;
        var monthEndUtc = bounds[^1].endUtc;

        var slice = await StatusPriorityComparer
            .OrderForHistory(db.PersonStatuses.AsNoTracking()
                .Include(s => s.StatusKind)
                .Where(s => s.IsActive && ids.Contains(s.PersonId) && s.OpenDate < monthEndUtc))
            .ToListAsync(ct);

        // Перші призначення
        var firstAssign = await db.PersonPositionAssignments.AsNoTracking()
            .Where(a => ids.Contains(a.PersonId))
            .GroupBy(a => a.PersonId)
            .Select(g => new { PersonId = g.Key, FirstOpenUtc = g.Min(x => x.OpenUtc) })
            .ToListAsync(ct);
        var firstAssignMap = firstAssign.ToDictionary(x => x.PersonId, x => x.FirstOpenUtc);

        var map = new Dictionary<Guid, PersonStatus?[]>(ids.Length);

        foreach (var pid in ids)
        {
            var items = slice
                .Where(s => s.PersonId == pid)
                .OrderBy(s => s.OpenDate)
                .ThenBy(s => s.StatusKind!, StatusKindPriorityComparer)
                .ThenBy(s => s.Id)
                .ToList();
            var row = new PersonStatus?[daysInMonth];

            DateTime? firstPresenceUtc = items.Count == 0 ? null : items.Min(s => s.OpenDate);
            if (firstAssignMap.TryGetValue(pid, out var fa))
                firstPresenceUtc = firstPresenceUtc is null ? fa : (fa < firstPresenceUtc ? fa : firstPresenceUtc);

            for (int di = 0; di < daysInMonth; di++)
            {
                var (dayStartUtc, dayEndUtc) = bounds[di];

                // “Не існує ще” → нб
                if (firstPresenceUtc is null || dayEndUtc <= firstPresenceUtc.Value)
                {
                    row[di] = kindNb is null ? null : Synthetic(kindNb, pid);
                    continue;
                }

                var baseline = items
                    .Where(x => x.OpenDate < dayStartUtc)
                    .OrderByDescending(x => x.OpenDate)
                    .ThenByDescending(x => x.Sequence)
                    .FirstOrDefault();

                PersonStatus? synthetic = baseline is null && kind30 is not null ? Synthetic(kind30, pid) : null;

                var inDay = items.Where(x => x.OpenDate >= dayStartUtc && x.OpenDate < dayEndUtc);

                var contenders = new List<PersonStatus>(8);
                if (baseline is not null) contenders.Add(baseline);
                if (synthetic is not null) contenders.Add(synthetic);
                contenders.AddRange(inDay);

                if (contenders.Count == 0)
                {
                    row[di] = kind30 is null ? null : Synthetic(kind30, pid);
                    continue;
                }

                var chosen = contenders
                    .OrderBy(c => c.StatusKind!, StatusKindPriorityComparer)
                    .ThenByDescending(c => c.OpenDate)
                    .ThenByDescending(c => c.Sequence)
                    .First();

                row[di] = chosen;
            }

            map[pid] = row;
        }

        return map;
    }

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

    private static PersonStatus Synthetic(StatusKind kind, Guid personId) => new()
    {
        Id = Guid.Empty,
        PersonId = personId,
        StatusKindId = kind.Id,
        StatusKind = kind,
        OpenDate = DateTime.MinValue,
        Sequence = short.MinValue,
        IsActive = true
    };
}