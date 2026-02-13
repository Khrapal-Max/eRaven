//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-repo:
/// 1) Місячна матриця табеля по всім особам (UI grid).
/// 2) Місячний табель однієї особи: Person + Entries (source of truth).
///
/// IMPORTANT (current stage):
/// - Timesheet is a "fact" only.
/// - TaskCodes are returned as empty strings (reserved for future task subsystem).
/// </summary>
public sealed class TimesheetMonthRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetMonthRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<TimesheetPersonMonthRowDto>> GetTimesheetMonthAsync(
        int year,
        int month,
        string? search,
        CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year));
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, daysInMonth);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) “Хто в табелі в цьому місяці” => будь-який timeline, який перетинає місяць
        var inMonthQ = Overlapping(db.TimesheetTimelines.AsNoTracking(), monthStart, monthEnd);

        var personIdsQ = inMonthQ
            .Select(t => t.PersonId)
            .Distinct();

        // 2) Люди (з пошуком)
        var personsQ = db.PersonRead.AsNoTracking()
            .Where(p => personIdsQ.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpperInvariant();
            personsQ = personsQ.Where(p =>
                (p.FullName ?? "").ToUpper().Contains(s) ||
                (p.Rnokpp ?? "").ToUpper().Contains(s));
        }

        var persons = await personsQ
            .OrderBy(p => p.EnrollmentKind)
            .ThenBy(p => p.PositionSort)
            .ThenBy(p => p.FullName)
            .Select(p => new
            {
                p.Id,
                p.FullName,
                p.Rnokpp,
                p.Rank,
                p.Position,
                p.EnrollmentKind,
                p.EnrolledAt,
                p.ExcludedAt
            })
            .ToListAsync(ct);

        if (persons.Count == 0)
            return [];

        var selectedPersonIds = persons.Select(x => x.Id).ToArray();

        // 3) Timelines для вибраних осіб, що перетинають місяць
        var timelinesInMonthQ = Overlapping(
            db.TimesheetTimelines.AsNoTracking()
                .Where(t => selectedPersonIds.Contains(t.PersonId)),
            monthStart, monthEnd);

        // 4) Entries перетинають місяць + належать timelinesInMonthQ (JOIN) + JOIN codes
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in timelinesInMonthQ on e.TimelineId equals t.Id
            join c in db.TimesheetCodes.AsNoTracking() on e.TimesheetCodeDefinitionId equals c.Id
            where !e.IsDeleted
                  && e.From <= monthEnd
                  && (!e.To.HasValue || e.To.Value >= monthStart)
            orderby e.PersonId, e.From, e.Id
            select new
            {
                e.PersonId,
                CodeId = c.Id,
                c.Code,
                e.From,
                e.To,
                e.Reference
            }
        ).ToListAsync(ct);

        var byPerson = entries
            .GroupBy(e => e.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5) Будуємо матрицю: default code = "НБ"
        var rows = new List<TimesheetPersonMonthRowDto>(persons.Count);

        foreach (var p in persons)
        {
            var dayCodes = new string[daysInMonth];
            var referenses = new string?[daysInMonth];

            for (var i = 0; i < daysInMonth; i++)
                dayCodes[i] = TimesheetSystemCodes.NotInTimesheet;

            if (byPerson.TryGetValue(p.Id, out var list))
            {
                foreach (var e in list)
                {
                    var start = e.From < monthStart ? monthStart : e.From;
                    var end = e.To is null
                        ? monthEnd
                        : (e.To.Value > monthEnd ? monthEnd : e.To.Value);

                    var code = TrimCode(e.Code);

                    for (var d = start; d <= end; d = d.AddDays(1))
                    {
                        var idx = d.Day - 1;
                        if ((uint)idx >= (uint)daysInMonth) continue;

                        dayCodes[idx] = string.IsNullOrWhiteSpace(code)
                            ? TimesheetSystemCodes.NotInTimesheet
                            : code;

                        referenses[idx] = IsAlert(code)
                            ? (string.IsNullOrWhiteSpace(e.Reference) ? null : e.Reference.Trim())
                            : null;
                    }
                }
            }

            rows.Add(new TimesheetPersonMonthRowDto(
                PersonId: p.Id,
                FullName: p.FullName ?? "",
                RNOKPP: p.Rnokpp ?? "",
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                Codes: dayCodes,
                Referenses: referenses
            ));
        }

        return rows;
    }

    public async Task<IReadOnlyList<TimesheetPersonRangeRowDto>> GetTimesheetRangeAsync(
        DateOnly from,
        DateOnly to,
        string? search,
        CancellationToken ct = default)
    {
        if (to < from) throw new ArgumentException("To must be >= From.", nameof(to));

        var days = to.DayNumber - from.DayNumber + 1;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // системний "НБ" як дефолт (але НЕ як подія)
        var nb = await db.TimesheetCodes.AsNoTracking()
            .Where(x => x.Code == TimesheetSystemCodes.NotInTimesheet)
            .Select(x => new { x.Id, x.Code })
            .SingleAsync(ct);

        var nbId = nb.Id;
        var nbCode = (nb.Code ?? TimesheetSystemCodes.NotInTimesheet).Trim();

        // 1) timelines, що перетинають [from..to]
        var timelinesQ = db.TimesheetTimelines.AsNoTracking()
            .Where(t => t.OpenedAt <= to && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= from));

        var personIdsQ = timelinesQ.Select(t => t.PersonId).Distinct();

        // 2) persons (з пошуком)
        var personsQ = db.PersonRead.AsNoTracking()
            .Where(p => personIdsQ.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpperInvariant();
            personsQ = personsQ.Where(p =>
                (p.FullName ?? "").ToUpper().Contains(s) ||
                (p.Rnokpp ?? "").ToUpper().Contains(s));
        }

        var persons = await personsQ
            .OrderBy(p => p.EnrollmentKind)
            .ThenBy(p => p.PositionSort)
            .ThenBy(p => p.FullName)
            .Select(p => new
            {
                p.Id,
                p.FullName,
                p.Rnokpp,
                p.Rank,
                p.Position,
                p.EnrollmentKind,
                p.EnrolledAt,
                p.ExcludedAt
            })
            .ToListAsync(ct);

        if (persons.Count == 0)
            return [];

        var selectedIds = persons.Select(x => x.Id).ToArray();

        var selectedTimelinesQ = timelinesQ.Where(t => selectedIds.Contains(t.PersonId));

        // 3) entries, що перетинають [from..to] + належать selectedTimelinesQ
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in selectedTimelinesQ on e.TimelineId equals t.Id
            join c in db.TimesheetCodes.AsNoTracking() on e.TimesheetCodeDefinitionId equals c.Id
            where !e.IsDeleted
                  && e.From <= to
                  && (!e.To.HasValue || e.To.Value >= @from)
            orderby e.PersonId, e.From, e.Id
            select new
            {
                e.PersonId,
                CodeId = c.Id,
                c.Code,
                e.From,
                e.To,
                e.Reference
            }
        ).ToListAsync(ct);

        var byPerson = entries
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<TimesheetPersonRangeRowDto>(persons.Count);

        foreach (var p in persons)
        {
            var dayDtos = new TimesheetRangeDayDto[days];

            for (var i = 0; i < days; i++)
            {
                var d = from.AddDays(i);
                dayDtos[i] = new TimesheetRangeDayDto(
                    Date: d,
                    CodeId: nbId,
                    Code: nbCode,
                    Reference: null);
            }

            if (byPerson.TryGetValue(p.Id, out var list))
            {
                foreach (var e in list)
                {
                    var start = e.From < from ? from : e.From;
                    var end = e.To is null ? to : (e.To.Value > to ? to : e.To.Value);

                    var code = (e.Code ?? "").Trim();

                    // ⚠️ важливо: якщо Code пустий — це NB і по CodeId теж має бути NB,
                    // інакше в drawer прилетить "лівий" id при відображенні "НБ".
                    var effectiveCodeId = code.Length == 0 ? nbId : e.CodeId;
                    var effectiveCode = code.Length == 0 ? nbCode : code;

                    var refForAlert = IsAlert(effectiveCode)
                        ? (string.IsNullOrWhiteSpace(e.Reference) ? null : e.Reference.Trim())
                        : null;

                    for (var d = start; d <= end; d = d.AddDays(1))
                    {
                        var idx = d.DayNumber - from.DayNumber;
                        if ((uint)idx >= (uint)days) continue;

                        dayDtos[idx] = new TimesheetRangeDayDto(
                            Date: d,
                            CodeId: effectiveCodeId,
                            Code: effectiveCode,
                            Reference: refForAlert);
                    }
                }
            }

            rows.Add(new TimesheetPersonRangeRowDto(
                PersonId: p.Id,
                FullName: p.FullName ?? "",
                RNOKPP: p.Rnokpp ?? "",
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                Days: dayDtos
            ));
        }

        return rows;
    }

    public async Task<TimesheetPersonMonthDto?> GetTimesheetPersonMonthAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty) throw new ArgumentException("PersonId is required.", nameof(personId));
        if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year));
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, daysInMonth);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) Person snapshot
        var p = await db.PersonRead
            .AsNoTracking()
            .Where(x => x.Id == personId)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.Rnokpp,
                x.Rank,
                x.Position,
                x.EnrollmentKind,
                x.EnrolledAt,
                x.ExcludedAt
            })
            .SingleOrDefaultAsync(ct);

        if (p is null)
            return null;

        // 2) Timelines that overlap the month — as query
        var timelinesInMonthQ = Overlapping(
            db.TimesheetTimelines.AsNoTracking().Where(t => t.PersonId == personId),
            monthStart, monthEnd);

        // 3) Entries overlapping month (JOIN) — source of truth + JOIN codes
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in timelinesInMonthQ on e.TimelineId equals t.Id
            join c in db.TimesheetCodes.AsNoTracking() on e.TimesheetCodeDefinitionId equals c.Id
            where !e.IsDeleted
                  && e.From <= monthEnd
                  && (!e.To.HasValue || e.To.Value >= monthStart)
            orderby e.From, e.Id
            select new
            {
                CodeId = c.Id,
                c.Code,
                e.From,
                e.To,
                e.Reference,
                e.Note,
                e.CreatedAtUtc,
                e.UpdatedAtUtc
            }
        ).ToListAsync(ct);

        var updatedAtUtc = entries.Count == 0
            ? DateTime.MinValue
            : entries.Max(x => x.UpdatedAtUtc ?? x.CreatedAtUtc);

        // 4) Day codes defaults
        var dayCodes = new string[daysInMonth];
        var referenses = new string?[daysInMonth];

        for (var i = 0; i < daysInMonth; i++)
            dayCodes[i] = TimesheetSystemCodes.NotInTimesheet;

        foreach (var e in entries)
        {
            var start = e.From < monthStart ? monthStart : e.From;
            var end = e.To is null ? monthEnd : (e.To.Value > monthEnd ? monthEnd : e.To.Value);

            var code = TrimCode(e.Code);

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var idx = d.Day - 1;
                if ((uint)idx >= (uint)daysInMonth) continue;

                dayCodes[idx] = string.IsNullOrWhiteSpace(code)
                    ? TimesheetSystemCodes.NotInTimesheet
                    : code;

                referenses[idx] = IsAlert(code)
                    ? (string.IsNullOrWhiteSpace(e.Reference) ? null : e.Reference.Trim())
                    : null;
            }
        }

        var personRow = new TimesheetPersonMonthRowDto(
            PersonId: p.Id,
            FullName: p.FullName ?? "",
            RNOKPP: p.Rnokpp ?? "",
            Rank: p.Rank,
            Position: p.Position,
            EnrollmentKind: p.EnrollmentKind,
            EnrolledAt: p.EnrolledAt,
            ExcludedAt: p.ExcludedAt,
            Codes: dayCodes,
            Referenses: referenses
        );

        var entryRows = entries.Select(e => new TimesheetPersonEntryRowDto(
            Code: TrimCode(e.Code),
            From: e.From,
            To: e.To,
            Reference: string.IsNullOrWhiteSpace(e.Reference) ? null : e.Reference.Trim(),
            Note: string.IsNullOrWhiteSpace(e.Note) ? null : e.Note.Trim()
        )).ToList();

        return new TimesheetPersonMonthDto(
            Person: personRow,
            Year: year,
            Month: month,
            DaysInMonth: daysInMonth,
            UpdatedAtUtc: updatedAtUtc,
            Entries: entryRows
        );
    }

    public async Task<IReadOnlyList<TimesheetPersonDayRowDto>> GetTimesheetDayAsync(
        DateOnly date,
        string? search,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) who is "in timesheet" on this date (by timeline overlap)
        var timelinesOnDateQ = db.TimesheetTimelines
            .AsNoTracking()
            .Where(t => t.OpenedAt <= date
                        && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= date));

        var personIdsQ = timelinesOnDateQ
            .Select(t => t.PersonId)
            .Distinct();

        // 2) persons (with optional search)
        var personsQ = db.PersonRead
            .AsNoTracking()
            .Where(p => personIdsQ.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToUpperInvariant();
            personsQ = personsQ.Where(p =>
                (p.FullName ?? "").ToUpper().Contains(s) ||
                (p.Rnokpp ?? "").ToUpper().Contains(s));
        }

        var persons = await personsQ
            .OrderBy(p => p.EnrollmentKind)
            .ThenBy(p => p.PositionSort)
            .ThenBy(p => p.FullName)
            .Select(p => new
            {
                p.Id,
                p.FullName,
                p.Rnokpp,
                p.Rank,
                p.Position,
                p.EnrollmentKind,
                p.EnrolledAt,
                p.ExcludedAt
            })
            .ToListAsync(ct);

        if (persons.Count == 0)
            return [];

        var selectedPersonIds = persons.Select(x => x.Id).ToArray();

        // 3) timelines for selected persons on date
        var timelinesQ = db.TimesheetTimelines
            .AsNoTracking()
            .Where(t => selectedPersonIds.Contains(t.PersonId)
                        && t.OpenedAt <= date
                        && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= date));

        // 4) active entries on date (join timelines) + join codes
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in timelinesQ on e.TimelineId equals t.Id
            join c in db.TimesheetCodes.AsNoTracking() on e.TimesheetCodeDefinitionId equals c.Id
            where !e.IsDeleted
                  && e.From <= date
                  && (!e.To.HasValue || e.To.Value >= date)
            orderby e.PersonId, e.From, e.Id
            select new
            {
                e.PersonId,
                CodeId = c.Id,
                c.Code,
                e.Reference,
                e.Note
            }
        ).ToListAsync(ct);

        // latest active per person (single stream)
        var lastByPerson = entries
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.Last());

        TimesheetDayStateDto DayState(Guid pid)
        {
            if (lastByPerson.TryGetValue(pid, out var v))
            {
                var code = TrimCode(v.Code);
                return new TimesheetDayStateDto(
                    CodeId: v.CodeId,
                    Code: code.Length == 0 ? TimesheetSystemCodes.NotInTimesheet : code,
                    Reference: string.IsNullOrWhiteSpace(v.Reference) ? null : v.Reference.Trim(),
                    Note: string.IsNullOrWhiteSpace(v.Note) ? null : v.Note.Trim()
                );
            }

            return new TimesheetDayStateDto(Guid.Empty, TimesheetSystemCodes.NotInTimesheet, null, null);
        }

        var rows = new List<TimesheetPersonDayRowDto>(persons.Count);
        foreach (var p in persons)
        {
            rows.Add(new TimesheetPersonDayRowDto(
                PersonId: p.Id,
                FullName: p.FullName ?? "",
                RNOKPP: p.Rnokpp ?? "",
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                DayState: DayState(p.Id)
            ));
        }

        return rows;
    }

    private static IQueryable<TimesheetTimeline> Overlapping(
        IQueryable<TimesheetTimeline> q,
        DateOnly from,
        DateOnly to)
        => q.Where(t => t.OpenedAt <= to && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= from));

    private static string TrimCode(string? code) => (code ?? "").Trim();

    private static bool IsAlert(string? code)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        return c == TimesheetSystemCodes.DoesTheCombatTask
            || c == TimesheetSystemCodes.InjuryFact;
    }
}
