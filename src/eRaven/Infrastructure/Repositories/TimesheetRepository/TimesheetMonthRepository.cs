//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthGridRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-repo:
/// 1) Місячна матриця табеля по всім особам (UI grid): rows із MainCodes/TaskCodes.
/// 2) Місячний табель однієї особи: Person + Entries (source of truth).
/// Оптимізація: JOIN по timelines (без витягування timelineIds в памʼять де це можливо).
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

        // 1) “Хто в табелі в цьому місяці” => по MAIN timeline (перетин з місяцем)
        var mainInMonthQ = Overlapping(
            db.TimesheetTimelines.AsNoTracking().Where(t => t.Lane == TimesheetLane.Main),
            monthStart, monthEnd);

        var personIdsQ = mainInMonthQ
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

        // 3) Timelines (MAIN+TASK) для вибраних осіб, що перетинають місяць
        var timelinesInMonthQ = Overlapping(
            db.TimesheetTimelines.AsNoTracking()
                .Where(t => selectedPersonIds.Contains(t.PersonId)),
            monthStart, monthEnd);

        // 4) Entries перетинають місяць + належать timelinesInMonthQ (JOIN)
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in timelinesInMonthQ on e.TimelineId equals t.Id
            where !e.IsDeleted
                  && e.From <= monthEnd
                  && (!e.To.HasValue || e.To.Value >= monthStart)
            orderby e.PersonId, e.Lane, e.From, e.Id
            select new
            {
                e.PersonId,
                e.Lane,
                e.Code,
                e.From,
                e.To
            }
        ).ToListAsync(ct);

        var byPerson = entries
            .GroupBy(e => e.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5) Будуємо матрицю (Main default = "НБ", Task default = "")
        var rows = new List<TimesheetPersonMonthRowDto>(persons.Count);

        foreach (var p in persons)
        {
            var main = new string[daysInMonth];
            var task = new string[daysInMonth];

            for (var i = 0; i < daysInMonth; i++)
            {
                main[i] = "НБ";
                task[i] = "";
            }

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

                        if (e.Lane == TimesheetLane.Main) main[idx] = code;
                        else if (e.Lane == TimesheetLane.Task) task[idx] = code;
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
                MainCodes: main,
                TaskCodes: task
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

        // 2) Timelines that overlap the month (both lanes) — as query
        var timelinesInMonthQ = Overlapping(
            db.TimesheetTimelines.AsNoTracking().Where(t => t.PersonId == personId),
            monthStart, monthEnd);

        // 3) Entries overlapping month (JOIN) — source of truth
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in timelinesInMonthQ on e.TimelineId equals t.Id
            where !e.IsDeleted
                  && e.From <= monthEnd
                  && (!e.To.HasValue || e.To.Value >= monthStart)
            orderby e.Lane, e.From, e.Id
            select new
            {
                e.Lane,
                e.Code,
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

        // 4) Day codes (grid) defaults
        var main = new string[daysInMonth];
        var task = new string[daysInMonth];

        for (var i = 0; i < daysInMonth; i++)
        {
            main[i] = "НБ";
            task[i] = "";
        }

        foreach (var e in entries)
        {
            var start = e.From < monthStart ? monthStart : e.From;
            var end = e.To is null ? monthEnd : (e.To.Value > monthEnd ? monthEnd : e.To.Value);

            var code = TrimCode(e.Code);

            for (var d = start; d <= end; d = d.AddDays(1))
            {
                var idx = d.Day - 1;
                if ((uint)idx >= (uint)daysInMonth) continue;

                if (e.Lane == TimesheetLane.Main) main[idx] = code;
                else if (e.Lane == TimesheetLane.Task) task[idx] = code;
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
            MainCodes: main,
            TaskCodes: task
        );

        var entryRows = entries.Select(e => new TimesheetPersonEntryRowDto(
            Lane: e.Lane,
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

    private static IQueryable<TimesheetTimeline> Overlapping(
        IQueryable<TimesheetTimeline> q,
        DateOnly from,
        DateOnly to)
        => q.Where(t => t.OpenedAt <= to && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= from));

    private static string TrimCode(string? code) => (code ?? "").Trim();
}