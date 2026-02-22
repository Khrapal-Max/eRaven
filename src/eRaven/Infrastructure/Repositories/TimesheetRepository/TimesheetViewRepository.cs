//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetViewRepository (segments in DB + day-matrix projection)
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Abstractions.TimesheetRepository.ReadModels;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-репозиторій табеля для UI/звітів.
///
/// <para>Важливе:</para>
/// <list type="bullet">
/// <item><description>повертає <b>timesheet-only</b> (без PersonRead)</description></item>
/// <item><description>у БД зберігаються інтервали (entries), а матриця по днях будується в памʼяті</description></item>
/// </list>
/// </summary>
public sealed class TimesheetViewRepository(IDbContextFactory<AppDbContext> dbFactory) : ITimesheetViewRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // Public API
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPeriodRm>> GetTimesheetsMonthAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Некоректний рік.");
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Некоректний місяць.");

        var fromDate = new DateOnly(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var toExclusive = fromDate.AddDays(daysInMonth);

        var periods = await GetTimesheetRangeInternalAsync(fromDate, toExclusive, personId: null, ct);

        return periods;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPeriodRm>> GetTimesheetsDayAsync(
        DateOnly date,
        CancellationToken ct = default)
    {
        var fromDate = date;
        var toExclusive = date.AddDays(1);
        var periods = await GetTimesheetRangeInternalAsync(fromDate, toExclusive, personId: null, ct);

        return periods;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPeriodRm>> GetTimesheetsRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct = default)
    {
        if (toDate < fromDate)
            throw new ArgumentOutOfRangeException(nameof(toDate), "toDate має бути >= fromDate.");

        var toExclusive = toDate.AddDays(1);
        var periods = await GetTimesheetRangeInternalAsync(fromDate, toExclusive, personId: null, ct);

        return periods;
    }

    /// <inheritdoc />
    public async Task<TimesheetPeriodRm?> GetTimesheetPersonMonthAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("personId is required.", nameof(personId));
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Некоректний рік.");
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Некоректний місяць.");

        var fromDate = new DateOnly(year, month, 1);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var toExclusive = fromDate.AddDays(daysInMonth);

        var periods = await GetTimesheetRangeInternalAsync(fromDate, toExclusive, personId, ct);
        return periods.Count == 0 ? null : periods[0];
    }

    //======================================================================
    // Internal range projection
    //======================================================================

    private async Task<IReadOnlyList<TimesheetPeriodRm>> GetTimesheetRangeInternalAsync(
    DateOnly fromDate,
    DateOnly toExclusive,
    Guid? personId,
    CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) Codes: dictionary for O(1) lookup
        var codesList = await db.TimesheetCodes
            .AsNoTracking()
            .Select(x => new { x.Id, x.Code })
            .ToListAsync(ct);

        var codeById = codesList.ToDictionary(x => x.Id, x => x.Code);
        var nbCodeId = codesList.FirstOrDefault(x => x.Code == TimesheetSystemCodes.NotInTimesheet)?.Id ?? Guid.Empty;

        // 2) Episodes that intersect [fromDate..toExclusive)
        var episodes = await db.TimeSheets
            .AsNoTracking()
            .Where(t => t.OpenedAt < toExclusive)
            .Where(t => !t.ClosedAt.HasValue || t.ClosedAt.Value.AddDays(1) > fromDate)
            .Include(t => t.Entries.Where(e => !e.IsDeleted))
            .ToListAsync(ct); // filtered include is ok

        if (personId is not null)
            episodes = [.. episodes.Where(t => t.PersonId == personId.Value)];

        var periodsRm = new List<TimesheetPeriodRm>(episodes.Count);

        foreach (var episode in episodes)
        {
            // 3) Clamp range to episode bounds
            var rangeStart = fromDate > episode.OpenedAt ? fromDate : episode.OpenedAt;

            var episodeEndExclusive = episode.ClosedAt.HasValue
                ? episode.ClosedAt.Value.AddDays(1)
                : toExclusive;

            var rangeEndExclusive = episodeEndExclusive < toExclusive ? episodeEndExclusive : toExclusive;

            if (rangeStart >= rangeEndExclusive)
            {
                periodsRm.Add(new TimesheetPeriodRm(episode.PersonId, []));
                continue;
            }

            // 4) Active entries sorted by From
            var entries = episode.Entries
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.From)
                .ToList();

            var daysCount = rangeEndExclusive.DayNumber - rangeStart.DayNumber;
            var days = new List<TimesheetDayRm>(daysCount);

            // 5) Find current entry active on rangeStart (exclusive To)
            TimesheetEntry? current = null;
            var nextIndex = 0;

            if (entries.Count > 0)
            {
                // move nextIndex to first entry with From > rangeStart,
                // while also capturing the last active entry at rangeStart
                for (var i = 0; i < entries.Count; i++)
                {
                    var e = entries[i];

                    if (e.From <= rangeStart)
                    {
                        nextIndex = i + 1;

                        // active if From <= d < To (or To == null)
                        if (!e.To.HasValue || rangeStart < e.To.Value)
                            current = e;

                        continue;
                    }

                    break;
                }
            }

            TimesheetEntry? next = nextIndex < entries.Count ? entries[nextIndex] : null;

            // 6) Expand to day-matrix; fill gaps with NB
            for (var d = rangeStart; d < rangeEndExclusive; d = d.AddDays(1))
            {
                // switch to next entry when its From is reached
                while (next is not null && d >= next.From)
                {
                    current = next;
                    nextIndex++;
                    next = nextIndex < entries.Count ? entries[nextIndex] : null;
                }

                // if current has To and we are outside [From..To) -> no active (gap) => NB
                if (current is not null && current.To.HasValue && d >= current.To.Value)
                    current = null;

                if (current is null)
                {
                    days.Add(new TimesheetDayRm(
                        TimesheetId: Guid.Empty,
                        DateOfDay: d,
                        CodeId: nbCodeId,
                        Code: TimesheetSystemCodes.NotInTimesheet,
                        Reference: null,
                        Note: null));
                    continue;
                }

                var code = codeById.TryGetValue(current.TimesheetCodeDefinitionId, out var c) ? c : string.Empty;

                days.Add(new TimesheetDayRm(
                    TimesheetId: current.TimesheetId,
                    DateOfDay: d,
                    CodeId: current.TimesheetCodeDefinitionId,
                    Code: code,
                    Reference: current.Reference,
                    Note: current.Note));
            }

            periodsRm.Add(new TimesheetPeriodRm(episode.PersonId, days));
        }

        return periodsRm;
    }
}