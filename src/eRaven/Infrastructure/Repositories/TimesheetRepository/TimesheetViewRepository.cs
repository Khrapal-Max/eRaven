//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetViewRepository (segments in DB + day-matrix projection)
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Abstractions.TimesheetRepository.ReadModels;
using eRaven.Domain.Consts;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
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
            .Select(x => new { x.Id, x.Code, x.UiStyle })
            .ToListAsync(ct);

        var codeById = codesList.ToDictionary(x => x.Id, x => (x.Code, x.UiStyle));

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

        // IMPORTANT: repository returns a full day-matrix for the requested range.
        // Days outside episode bounds must be represented as derived NB.
        // This avoids index shifts in UI (e.g. enroll mid-month) and keeps the matrix stable.

        var totalDays = toExclusive.DayNumber - fromDate.DayNumber;
        var episodesByPerson = episodes
            .GroupBy(x => x.PersonId)
            .ToList();

        foreach (var group in episodesByPerson)
        {
            // 3) Initialize full range as derived NB
            var days = new List<TimesheetDayRm>(totalDays);
            for (var d = fromDate; d < toExclusive; d = d.AddDays(1))
            {
                days.Add(new TimesheetDayRm(
                    TimesheetId: Guid.Empty,
                    DateOfDay: d,
                    CodeId: null,
                    Code: TimesheetDerivedCodes.NotInTimesheet,
                    Reference: null,
                    Note: null,
                    IsDerived: true,
                    IsChangePoint: false,
                    UiStyle: TimesheetUiStyle.NotInTimesheet));
            }

            // 4) Overlay each episode segment into the full matrix
            foreach (var episode in group.OrderBy(x => x.OpenedAt))
            {
                var episodeEndExclusive = episode.ClosedAt.HasValue
                    ? episode.ClosedAt.Value.AddDays(1)
                    : toExclusive;

                var segmentStart = fromDate > episode.OpenedAt ? fromDate : episode.OpenedAt;
                var segmentEndExclusive = episodeEndExclusive < toExclusive ? episodeEndExclusive : toExclusive;

                if (segmentStart >= segmentEndExclusive)
                    continue;

                // 5) Active entries sorted by From
                var entries = episode.Entries
                    .Where(e => !e.IsDeleted)
                    .OrderBy(e => e.From)
                    .ToList();

                // 6) Find current entry active on segmentStart (exclusive To)
                TimesheetEntry? current = null;
                var nextIndex = 0;

                if (entries.Count > 0)
                {
                    for (var i = 0; i < entries.Count; i++)
                    {
                        var e = entries[i];

                        if (e.From <= segmentStart)
                        {
                            nextIndex = i + 1;

                            // active if From <= d < To (or To == null)
                            if (!e.To.HasValue || segmentStart < e.To.Value)
                                current = e;

                            continue;
                        }

                        break;
                    }
                }

                TimesheetEntry? next = nextIndex < entries.Count ? entries[nextIndex] : null;

                // 7) Fill the episode segment [segmentStart..segmentEndExclusive)
                for (var d = segmentStart; d < segmentEndExclusive; d = d.AddDays(1))
                {
                    // switch to next entry when its From is reached
                    while (next is not null && d >= next.From)
                    {
                        current = next;
                        nextIndex++;
                        next = nextIndex < entries.Count ? entries[nextIndex] : null;
                    }

                    // if current has To and we are outside [From..To) -> no active (gap)
                    if (current is not null && current.To.HasValue && d >= current.To.Value)
                        current = null;

                    var index = d.DayNumber - fromDate.DayNumber;

                    // NOTE: within an episode, gaps should not exist under normal invariants.
                    // If data is inconsistent, we fall back to derived NB.
                    if (current is null)
                    {
                        days[index] = new TimesheetDayRm(
                            TimesheetId: Guid.Empty,
                            DateOfDay: d,
                            CodeId: null,
                            Code: TimesheetDerivedCodes.NotInTimesheet,
                            Reference: null,
                            Note: null,
                            IsDerived: true,
                            IsChangePoint: false,
                            UiStyle: TimesheetUiStyle.NotInTimesheet);
                        continue;
                    }

                    var (code, uiStyle) = codeById.TryGetValue(current.TimesheetCodeDefinitionId, out var tuple)
                        ? tuple
                        : (string.Empty, TimesheetUiStyle.Warning);

                    var isChangePoint = d == current.From;

                    days[index] = new TimesheetDayRm(
                        TimesheetId: current.TimesheetId,
                        DateOfDay: d,
                        CodeId: current.TimesheetCodeDefinitionId,
                        Code: code,
                        Reference: current.Reference,
                        Note: current.Note,
                        IsDerived: false,
                        IsChangePoint: isChangePoint,
                        UiStyle: uiStyle);
                }
            }

            periodsRm.Add(new TimesheetPeriodRm(group.Key, days));
        }

        return periodsRm;
    }
}