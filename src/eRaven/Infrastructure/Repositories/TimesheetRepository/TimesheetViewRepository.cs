//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetViewRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-repo:
/// 1) Місячна матриця табеля по всім особам (UI grid).
/// 2) Місячний табель однієї особи: Person + Entries (manual події) + overlay TaskSpans.
/// 3) Денні/діапазонні вибірки для Operational UI.
///
/// <para>
/// Важливо:
/// <list type="bullet">
/// <item><description><see cref="TimesheetEntry"/> — джерело правди для ручних подій.</description></item>
/// <item><description><see cref="TimesheetTaskSpan"/> — факт “на завданні” (код 100), формується документом.</description></item>
/// <item><description>У read-моделі TaskSpans накладаються поверх Entries (щоб UI бачив 100 і референс документа).</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetViewRepository(IDbContextFactory<AppDbContext> dbFactory)
    : ITimesheetViewRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
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

        // 1) “Хто в табелі в цьому місяці” => будь-який episode, який перетинає місяць
        var inMonthQ = Overlapping(db.TimeSheets.AsNoTracking(), monthStart, monthEnd);

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

        // 3) Episodes для вибраних осіб, що перетинають місяць
        var episodesInMonthQ = Overlapping(
            db.TimeSheets.AsNoTracking()
                .Where(t => selectedPersonIds.Contains(t.PersonId)),
            monthStart, monthEnd);

        // 4) Entries перетинають місяць + належать episodesInMonthQ (JOIN) + JOIN codes
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in episodesInMonthQ on e.TimesheetId equals t.Id
            join c in db.TimesheetCodes.AsNoTracking() on e.TimesheetCodeDefinitionId equals c.Id
            where !e.IsDeleted
                  && e.From <= monthEnd
                  && (!e.To.HasValue || e.To.Value >= monthStart)
            orderby e.PersonId, e.From, e.Id
            select new
            {
                e.PersonId,
                c.Code,
                e.From,
                e.To,
                e.Reference
            }
        ).ToListAsync(ct);

        var byPersonEntries = entries
            .GroupBy(e => e.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 4.1) TaskSpans overlay (100 + reference)
        var spans = await LoadTaskSpanOverlaysAsync(db, episodesInMonthQ, monthStart, monthEnd, ct);
        var byPersonSpans = spans
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 5) Будуємо матрицю: default code = "НБ"
        var rows = new List<TimesheetPersonMonthRowDto>(persons.Count);

        foreach (var p in persons)
        {
            var dayCodes = new string[daysInMonth];
            var referenses = new string?[daysInMonth];

            for (var i = 0; i < daysInMonth; i++)
                dayCodes[i] = TimesheetSystemCodes.NotInTimesheet;

            // 5.1) Entries (manual/events)
            if (byPersonEntries.TryGetValue(p.Id, out var list))
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

                        // reference показуємо лише для alert-кодів
                        referenses[idx] = IsAlert(code)
                            ? (string.IsNullOrWhiteSpace(e.Reference) ? null : e.Reference.Trim())
                            : null;
                    }
                }
            }

            // 5.2) TaskSpans overlay (100 поверх entries)
            if (byPersonSpans.TryGetValue(p.Id, out var spanList))
                ApplyTaskOverlayMonth(spanList, monthStart, monthEnd, dayCodes, referenses);

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

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPersonRangeRowDto>> GetTimesheetRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string? search,
        CancellationToken ct = default)
    {
        if (toDate < fromDate) throw new ArgumentException("To must be >= From.", nameof(toDate));

        var days = toDate.DayNumber - fromDate.DayNumber + 1;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // системний "НБ" як дефолт (але НЕ як подія)
        var nb = await db.TimesheetCodes.AsNoTracking()
            .Where(x => x.Code == TimesheetSystemCodes.NotInTimesheet)
            .Select(x => new { x.Id, x.Code })
            .SingleAsync(ct);

        var nbId = nb.Id;
        var nbCode = (nb.Code ?? TimesheetSystemCodes.NotInTimesheet).Trim();

        // код 100 (для overlay)
        var taskDef = await db.TimesheetCodes.AsNoTracking()
            .Where(x => x.Code == TimesheetSystemCodes.DoesTheCombatTask)
            .Select(x => new { x.Id, x.Code })
            .SingleAsync(ct);

        var taskCodeId = taskDef.Id;
        var taskCode = (taskDef.Code ?? TimesheetSystemCodes.DoesTheCombatTask).Trim();

        // 1) episodes, що перетинають [from..to]
        var episodesQ = db.TimeSheets.AsNoTracking()
            .Where(t => t.OpenedAt <= toDate && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= fromDate));

        var personIdsQ = episodesQ.Select(t => t.PersonId).Distinct();

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

        var selectedEpisodesQ = episodesQ.Where(t => selectedIds.Contains(t.PersonId));

        // 3) entries, що перетинають [from..to] + належать selectedEpisodesQ
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in selectedEpisodesQ on e.TimesheetId equals t.Id
            join c in db.TimesheetCodes.AsNoTracking() on e.TimesheetCodeDefinitionId equals c.Id
            where !e.IsDeleted
                  && e.From <= toDate
                  && (!e.To.HasValue || e.To.Value >= fromDate)
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

        var byPersonEntries = entries
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 3.1) TaskSpans overlay
        var spans = await LoadTaskSpanOverlaysAsync(db, selectedEpisodesQ, fromDate, toDate, ct);
        var byPersonSpans = spans
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<TimesheetPersonRangeRowDto>(persons.Count);

        foreach (var p in persons)
        {
            var dayDtos = new TimesheetRangeDayDto[days];

            for (var i = 0; i < days; i++)
            {
                var d = fromDate.AddDays(i);
                dayDtos[i] = new TimesheetRangeDayDto(
                    Date: d,
                    CodeId: nbId,
                    Code: nbCode,
                    Reference: null);
            }

            // Entries
            if (byPersonEntries.TryGetValue(p.Id, out var list))
            {
                foreach (var e in list)
                {
                    var start = e.From < fromDate ? fromDate : e.From;
                    var end = e.To is null ? toDate : (e.To.Value > toDate ? toDate : e.To.Value);

                    var code = (e.Code ?? "").Trim();

                    // якщо Code пустий — це NB і по CodeId теж має бути NB
                    var effectiveCodeId = code.Length == 0 ? nbId : e.CodeId;
                    var effectiveCode = code.Length == 0 ? nbCode : code;

                    var refForAlert = IsAlert(effectiveCode)
                        ? (string.IsNullOrWhiteSpace(e.Reference) ? null : e.Reference.Trim())
                        : null;

                    for (var d = start; d <= end; d = d.AddDays(1))
                    {
                        var idx = d.DayNumber - fromDate.DayNumber;
                        if ((uint)idx >= (uint)days) continue;

                        dayDtos[idx] = new TimesheetRangeDayDto(
                            Date: d,
                            CodeId: effectiveCodeId,
                            Code: effectiveCode,
                            Reference: refForAlert);
                    }
                }
            }

            // TaskSpans overlay (100)
            if (byPersonSpans.TryGetValue(p.Id, out var spanList))
                ApplyTaskOverlayRange(spanList, fromDate, toDate, dayDtos, taskCodeId, taskCode);

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

    /// <inheritdoc />
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

        // 2) Episodes that overlap the month — as query
        var episodesInMonthQ = Overlapping(
            db.TimeSheets.AsNoTracking().Where(t => t.PersonId == personId),
            monthStart, monthEnd);

        // 3) Entries overlapping month (JOIN) — source of truth + JOIN codes
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in episodesInMonthQ on e.TimesheetId equals t.Id
            join c in db.TimesheetCodes.AsNoTracking() on e.TimesheetCodeDefinitionId equals c.Id
            where !e.IsDeleted
                  && e.From <= monthEnd
                  && (!e.To.HasValue || e.To.Value >= monthStart)
            orderby e.From, e.Id
            select new
            {
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

        // 4.1) TaskSpans overlay for this person/month
        var spans = await LoadTaskSpanOverlaysAsync(db, episodesInMonthQ, monthStart, monthEnd, ct);
        ApplyTaskOverlayMonth(spans, monthStart, monthEnd, dayCodes, referenses);

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

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPersonDayRowDto>> GetTimesheetDayAsync(
        DateOnly date,
        string? search,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) who is "in timesheet" on this date (by episode overlap)
        var episodesOnDateQ = db.TimeSheets
            .AsNoTracking()
            .Where(t => t.OpenedAt <= date
                        && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= date));

        var personIdsQ = episodesOnDateQ
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

        // 3) episodes for selected persons on date
        var selectedEpisodesQ = episodesOnDateQ
            .Where(t => selectedPersonIds.Contains(t.PersonId));

        // 4) active entries on date (join episodes) + join codes
        var entries = await (
            from e in db.TimesheetEntries.AsNoTracking()
            join t in selectedEpisodesQ on e.TimesheetId equals t.Id
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

        // 4.1) task spans active on this date
        var spans = await LoadTaskSpanOverlaysAsync(db, selectedEpisodesQ, date, date, ct);
        var spanByPerson = spans
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.First());

        TimesheetDayStateDto DayState(Guid pid)
        {
            // TaskSpan має пріоритет (100)
            if (spanByPerson.TryGetValue(pid, out var s))
            {
                var reference = SelectReferenceForDay(s, date);
                return new TimesheetDayStateDto(
                    CodeId: Guid.Empty, // Day view зараз не потребує CodeId, але залишаємо для сумісності
                    Code: TimesheetSystemCodes.DoesTheCombatTask,
                    Reference: reference,
                    Note: null);
            }

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

    //======================================================================
    // Task overlay helpers
    //======================================================================

    private sealed record TaskSpanOverlay(
        Guid PersonId,
        Guid MissionId,
        DateOnly FromDate,
        DateOnly? ToDateExclusive,
        string? OpenReference,
        string? CloseReference);

    /// <summary>
    /// Завантажує TaskSpans для періоду (inclusive) та готує overlay-дані:
    /// reference для <c>100</c> береться з <c>CombatTaskDocument.OrderTitle</c>.
    /// </summary>
    private static async Task<IReadOnlyList<TaskSpanOverlay>> LoadTaskSpanOverlaysAsync(
        AppDbContext db,
        IQueryable<TimeSheetAggregate> episodesQ,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken ct)
    {
        // NOTE: ToDate — exclusive, тому overlap: ToDate > from (а не >=)
        var raw = await (
            from s in db.TimesheetTaskSpans.AsNoTracking()
            join t in episodesQ on s.TimesheetId equals t.Id
            where s.Status != DocumentStatus.Canceled
                  && s.FromDate <= toDate
                  && (!s.ToDate.HasValue || s.ToDate.Value > fromDate)
            select new
            {
                s.PersonId,
                s.MissionId,
                s.FromDate,
                s.ToDate,
                s.OpenedByCombatTaskDocumentId,
                s.ClosedByCombatTaskDocumentId
            }
        ).ToListAsync(ct);

        if (raw.Count == 0)
            return [];

        var docIds = raw
            .Select(x => x.OpenedByCombatTaskDocumentId)
            .Concat(raw.Where(x => x.ClosedByCombatTaskDocumentId.HasValue).Select(x => x.ClosedByCombatTaskDocumentId!.Value))
            .Distinct()
            .ToList();

        // Reference для 100: виключно "назва документа"
        // (у твоїй моделі це CombatTaskDocument.OrderTitle)
        var docTitles = await db.CombatTaskDocuments.AsNoTracking()
            .Where(d => docIds.Contains(d.Id))
            .Select(d => new { d.Id, d.OrderTitle })
            .ToListAsync(ct);

        var docTitleMap = docTitles
            .Where(x => !string.IsNullOrWhiteSpace(x.OrderTitle))
            .ToDictionary(x => x.Id, x => x.OrderTitle!.Trim());

        static string? PickDocTitle(Dictionary<Guid, string> byDoc, Guid docId)
            => byDoc.TryGetValue(docId, out var t) && !string.IsNullOrWhiteSpace(t) ? t.Trim() : null;

        var result = new List<TaskSpanOverlay>(raw.Count);

        foreach (var x in raw)
        {
            var openRef = PickDocTitle(docTitleMap, x.OpenedByCombatTaskDocumentId);

            string? closeRef = null;
            if (x.ClosedByCombatTaskDocumentId.HasValue)
                closeRef = PickDocTitle(docTitleMap, x.ClosedByCombatTaskDocumentId.Value);

            // fallback: якщо closeRef порожній, але openRef є — використовуємо openRef
            closeRef ??= openRef;

            result.Add(new TaskSpanOverlay(
                PersonId: x.PersonId,
                MissionId: x.MissionId,
                FromDate: x.FromDate,
                ToDateExclusive: x.ToDate,
                OpenReference: openRef,
                CloseReference: closeRef));
        }

        return result;
    }

    /// <summary>
    /// Накладає task spans (код 100) на місячні масиви Codes/References.
    /// </summary>
    private static void ApplyTaskOverlayMonth(
        IReadOnlyList<TaskSpanOverlay> spans,
        DateOnly monthStart,
        DateOnly monthEnd,
        string[] dayCodes,
        string?[] references)
    {
        var monthEndExclusive = monthEnd.AddDays(1);

        foreach (var s in spans)
        {
            var start = s.FromDate < monthStart ? monthStart : s.FromDate;

            var endExclusive = s.ToDateExclusive ?? monthEndExclusive;
            if (endExclusive > monthEndExclusive) endExclusive = monthEndExclusive;

            if (endExclusive <= start)
                continue;

            // базовий референс для 100
            var openRef = string.IsNullOrWhiteSpace(s.OpenReference) ? null : s.OpenReference.Trim();

            for (var d = start; d < endExclusive; d = d.AddDays(1))
            {
                var idx = d.Day - 1;
                if ((uint)idx >= (uint)dayCodes.Length) continue;

                dayCodes[idx] = TimesheetSystemCodes.DoesTheCombatTask;
                references[idx] = openRef;
            }

            // якщо є "closing document" — для останнього дня span ставимо closing reference
            if (s.ToDateExclusive.HasValue && !string.IsNullOrWhiteSpace(s.CloseReference))
            {
                var lastDay = s.ToDateExclusive.Value.AddDays(-1);
                if (lastDay >= monthStart && lastDay <= monthEnd)
                {
                    var idx = lastDay.Day - 1;
                    if ((uint)idx < (uint)references.Length)
                        references[idx] = s.CloseReference.Trim();
                }
            }
        }
    }

    /// <summary>
    /// Накладає task spans (код 100) на діапазонні day DTO.
    /// </summary>
    private static void ApplyTaskOverlayRange(
        IReadOnlyList<TaskSpanOverlay> spans,
        DateOnly fromDate,
        DateOnly toDate,
        TimesheetRangeDayDto[] days,
        Guid taskCodeId,
        string taskCode)
    {
        var toExclusive = toDate.AddDays(1);

        foreach (var s in spans)
        {
            var start = s.FromDate < fromDate ? fromDate : s.FromDate;

            var endExclusive = s.ToDateExclusive ?? toExclusive;
            if (endExclusive > toExclusive) endExclusive = toExclusive;

            if (endExclusive <= start)
                continue;

            var openRef = string.IsNullOrWhiteSpace(s.OpenReference) ? null : s.OpenReference.Trim();

            for (var d = start; d < endExclusive; d = d.AddDays(1))
            {
                var idx = d.DayNumber - fromDate.DayNumber;
                if ((uint)idx >= (uint)days.Length) continue;

                var refForDay = openRef;

                if (s.ToDateExclusive.HasValue && d == s.ToDateExclusive.Value.AddDays(-1) && !string.IsNullOrWhiteSpace(s.CloseReference))
                    refForDay = s.CloseReference.Trim();

                days[idx] = new TimesheetRangeDayDto(
                    Date: d,
                    CodeId: taskCodeId,
                    Code: taskCode,
                    Reference: refForDay);
            }
        }
    }

    /// <summary>
    /// Повертає референс для конкретного дня (для day-view): openRef або closeRef на останній день.
    /// </summary>
    private static string? SelectReferenceForDay(TaskSpanOverlay s, DateOnly date)
    {
        var openRef = string.IsNullOrWhiteSpace(s.OpenReference) ? null : s.OpenReference.Trim();

        if (s.ToDateExclusive.HasValue
            && date == s.ToDateExclusive.Value.AddDays(-1)
            && !string.IsNullOrWhiteSpace(s.CloseReference))
        {
            return s.CloseReference.Trim();
        }

        return openRef;
    }

    //======================================================================
    // Common helpers
    //======================================================================

    private static IQueryable<TimeSheetAggregate> Overlapping(
        IQueryable<TimeSheetAggregate> q,
        DateOnly fromDate,
        DateOnly toDate)
        => q.Where(t => t.OpenedAt <= toDate && (!t.ClosedAt.HasValue || t.ClosedAt.Value >= fromDate));

    private static string TrimCode(string? code) => (code ?? "").Trim();

    private static bool IsAlert(string? code)
    {
        var c = (code ?? "").Trim().ToUpperInvariant();
        return c == TimesheetSystemCodes.DoesTheCombatTask
            || c == TimesheetSystemCodes.InjuryFact;
    }
}
