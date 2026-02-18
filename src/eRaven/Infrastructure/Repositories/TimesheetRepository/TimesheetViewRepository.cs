//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetViewRepository (segments in DB + matrix in memory)
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-репозиторій табеля для UI/звітів.
/// 
/// Принцип: у БД зберігаємо інтервали (entries/taskspans), а матрицю по днях (7/31) будуємо в памʼяті.
/// Це прибирає важкі SQL-матриці, крос-джойни й “денні” проміжні сутності.
/// </summary>
public sealed class TimesheetViewRepository(IDbContextFactory<AppDbContext> dbFactory) : ITimesheetViewRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    //======================================================================
    // GetTimesheetMonthAsync
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPersonMonthRowDto>> GetTimesheetMonthAsync(
        int year,
        int month,
        string? search,
        CancellationToken ct = default)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Некоректний рік.");
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Некоректний місяць.");

        var fromDate = new DateOnly(year, month, 1);
        var days = DateTime.DaysInMonth(year, month);
        var toExclusive = fromDate.AddDays(days);

        var data = await LoadRangeDataAsync(fromDate, toExclusive, search, mode: RangeMode.AnyOverlap, ct);

        var result = new List<TimesheetPersonMonthRowDto>(data.Persons.Count);

        foreach (var p in data.Persons.OrderBy(x => x.PositionSort))
        {
            var snaps = BuildSnapshotsForPerson(
                p,
                data,
                fromDate,
                toExclusive,
                includeNote: false);

            var codes = new string[days];
            var refs = new string?[days];

            for (var i = 0; i < days; i++)
            {
                codes[i] = snaps[i].Code;
                refs[i] = snaps[i].Reference;
            }

            result.Add(new TimesheetPersonMonthRowDto(
                PersonId: p.PersonId,
                FullName: p.FullName,
                RNOKPP: p.Rnokpp,
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                Codes: codes,
                Referenses: refs
            ));
        }

        return result;
    }

    //======================================================================
    // GetTimesheetRangeAsync (inclusive API, half-open internal)
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPersonRangeRowDto>> GetTimesheetRangeAsync(
        DateOnly fromDate,
        DateOnly toDate,
        string? search,
        CancellationToken ct = default)
    {
        if (toDate < fromDate)
            throw new ArgumentOutOfRangeException(nameof(toDate), "toDate має бути >= fromDate.");

        var toExclusive = toDate.AddDays(1);

        var data = await LoadRangeDataAsync(fromDate, toExclusive, search, mode: RangeMode.AnyOverlap, ct);

        var days = toExclusive.DayNumber - fromDate.DayNumber;
        var result = new List<TimesheetPersonRangeRowDto>(data.Persons.Count);

        foreach (var p in data.Persons.OrderBy(x => x.PositionSort))
        {
            var snaps = BuildSnapshotsForPerson(
                p,
                data,
                fromDate,
                toExclusive,
                includeNote: false);

            var dayDtos = new List<TimesheetRangeDayDto>(days);
            for (var i = 0; i < days; i++)
            {
                var d = fromDate.AddDays(i);
                dayDtos.Add(new TimesheetRangeDayDto(
                    Date: d,
                    CodeId: snaps[i].CodeId,
                    Code: snaps[i].Code,
                    Reference: snaps[i].Reference
                ));
            }

            result.Add(new TimesheetPersonRangeRowDto(
                PersonId: p.PersonId,
                FullName: p.FullName,
                RNOKPP: p.Rnokpp,
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                Days: dayDtos
            ));
        }

        return result;
    }

    //======================================================================
    // GetTimesheetPersonMonthAsync
    //======================================================================

    /// <inheritdoc />
    public async Task<TimesheetPersonMonthDto?> GetTimesheetPersonMonthAsync(
        Guid personId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (personId == Guid.Empty)
            throw new ArgumentException("personId is required.", nameof(personId));

        var fromDate = new DateOnly(year, month, 1);
        var days = DateTime.DaysInMonth(year, month);
        var toExclusive = fromDate.AddDays(days);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // 1) Person (single)
        var p = await db.PersonRead
            .AsNoTracking()
            .Where(x => x.Id == personId)
            .Select(x => new PersonInfo(
                PersonId: x.Id,
                Rnokpp: x.Rnokpp,
                FullName: x.FullName,
                Rank: x.Rank,
                PositionSort: x.PositionSort,
                Position: x.Position,
                EnrollmentKind: x.EnrollmentKind,
                EnrolledAt: x.EnrolledAt,
                ExcludedAt: x.ExcludedAt))
            .SingleOrDefaultAsync(ct);

        if (p is null)
            return null;

        // 2) Codes dictionary (needed for mapping + NB/task)
        var codes = await LoadCodesAsync(db, ct);

        // 3) Episodes in month (could be 0..N)
        var episodes = await db.TimeSheets
             .AsNoTracking()
             .Where(t => t.PersonId == personId)
             .Where(t => t.OpenedAt < toExclusive)
             .Where(t => !t.ClosedAt.HasValue || t.ClosedAt.Value.AddDays(1) > fromDate)
             .OrderBy(t => t.OpenedAt) // ✅ order BEFORE projection
             .Select(t => new EpisodeInfo(
                 TimesheetId: t.Id,
                 PersonId: t.PersonId,
                 OpenedAt: t.OpenedAt,
                 ClosedAt: t.ClosedAt))
             .ToListAsync(ct);

        var tsIds = episodes.Select(x => x.TimesheetId).Distinct().ToArray();

        // 4) Entries (history list)
        var entrySegs = tsIds.Length == 0
            ? []
            : await db.TimesheetEntries
                .AsNoTracking()
                .Where(e => tsIds.Contains(e.TimesheetId))
                .Where(e => !e.IsDeleted)
                .Where(e => e.From < toExclusive && (!e.To.HasValue || e.To.Value > fromDate)) // half-open overlap
                .OrderBy(e => e.From).ThenBy(e => e.Id)
                .Select(e => new EntrySeg(
                    Id: e.Id,
                    TimesheetId: e.TimesheetId,
                    PersonId: e.PersonId,
                    CodeId: e.TimesheetCodeDefinitionId,
                    From: e.From,
                    ToExclusive: e.To,
                    Reference: e.Reference,
                    Note: e.Note,
                    CreatedAtUtc: e.CreatedAtUtc,
                    UpdatedAtUtc: e.UpdatedAtUtc))
                .ToListAsync(ct);

        // 5) Task spans (optional overlay)
        var taskSegs = tsIds.Length == 0
            ? []
            : await db.TimesheetTaskSpans
                .AsNoTracking()
                .Where(s => tsIds.Contains(s.TimesheetId))
                .Where(s => s.Status != DocumentStatus.Canceled)
                .Where(s => s.FromDate < toExclusive && (!s.ToDate.HasValue || s.ToDate.Value > fromDate))
                .OrderBy(s => s.FromDate).ThenBy(s => s.Id)
                .Select(s => new TaskSeg(
                    Id: s.Id,
                    TimesheetId: s.TimesheetId,
                    PersonId: s.PersonId,
                    From: s.FromDate,
                    ToExclusive: s.ToDate))
                .ToListAsync(ct);

        // 6) Build snapshots for the month
        var data = new RangeData(
            Codes: codes,
            Persons: [p],
            EpisodesByPerson: new Dictionary<Guid, List<EpisodeInfo>> { [p.PersonId] = episodes },
            EntriesByTimesheet: GroupEntries(entrySegs),
            TasksByTimesheet: GroupTasks(taskSegs));

        var snaps = BuildSnapshotsForPerson(p, data, fromDate, toExclusive, includeNote: true);

        var codesArr = new string[days];
        var refsArr = new string?[days];

        for (var i = 0; i < days; i++)
        {
            codesArr[i] = snaps[i].Code;
            refsArr[i] = snaps[i].Reference;
        }

        var row = new TimesheetPersonMonthRowDto(
            PersonId: p.PersonId,
            FullName: p.FullName,
            RNOKPP: p.Rnokpp,
            Rank: p.Rank,
            Position: p.Position,
            EnrollmentKind: p.EnrollmentKind,
            EnrolledAt: p.EnrolledAt,
            ExcludedAt: p.ExcludedAt,
            Codes: codesArr,
            Referenses: refsArr
        );

        // UpdatedAtUtc = max(UpdatedAtUtc ?? CreatedAtUtc) over entries in month
        var updatedAtUtc = entrySegs.Count == 0
            ? p.EnrolledAt.HasValue ? DateTime.MinValue : DateTime.MinValue
            : entrySegs.Max(x => x.UpdatedAtUtc ?? x.CreatedAtUtc);

        // Entries list (as-is, half-open ToExclusive)
        var entryDtos = entrySegs
            .OrderBy(x => x.From).ThenBy(x => x.Id)
            .Select(x => new TimesheetPersonEntryRowDto(
                Code: codes.CodeById(x.CodeId),
                From: x.From,
                To: x.ToExclusive, // half-open
                Reference: string.IsNullOrWhiteSpace(x.Reference) ? null : x.Reference.Trim(),
                Note: string.IsNullOrWhiteSpace(x.Note) ? null : x.Note.Trim()))
            .ToList();

        return new TimesheetPersonMonthDto(
            Year: year,
            Month: month,
            DaysInMonth: days,
            UpdatedAtUtc: updatedAtUtc,
            Person: row,
            Entries: entryDtos
        );
    }

    //======================================================================
    // GetTimesheetDayAsync
    //======================================================================

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimesheetPersonDayRowDto>> GetTimesheetDayAsync(
        DateOnly date,
        string? search,
        CancellationToken ct = default)
    {
        var fromDate = date;
        var toExclusive = date.AddDays(1);

        var data = await LoadRangeDataAsync(fromDate, toExclusive, search, mode: RangeMode.ActiveOnFromDate, ct);

        var result = new List<TimesheetPersonDayRowDto>(data.Persons.Count);

        foreach (var p in data.Persons.OrderBy(x => x.FullName).ThenBy(x => x.Rnokpp))
        {
            var snaps = BuildSnapshotsForPerson(
                p,
                data,
                fromDate,
                toExclusive,
                includeNote: true);

            var s = snaps[0];

            result.Add(new TimesheetPersonDayRowDto(
                PersonId: p.PersonId,
                FullName: p.FullName,
                RNOKPP: p.Rnokpp,
                Rank: p.Rank,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt,
                DayState: new TimesheetDayStateDto(
                    CodeId: s.CodeId,
                    Code: s.Code,
                    Reference: s.Reference,
                    Note: s.Note)
            ));
        }

        return result;
    }

    //======================================================================
    // Core loading (episodes + segments)
    //======================================================================

    private enum RangeMode
    {
        AnyOverlap,
        ActiveOnFromDate
    }

    /// <summary>
    /// Завантажує дані для побудови матриці на діапазоні <paramref name="fromDate"/>..<paramref name="toExclusive"/> (half-open).
    /// </summary>
    private async Task<RangeData> LoadRangeDataAsync(
        DateOnly fromDate,
        DateOnly toExclusive,
        string? search,
        RangeMode mode,
        CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var codes = await LoadCodesAsync(db, ct);

        // Episodes filter:
        // overlap [from..toExclusive) with episode [OpenedAt..ClosedAtInclusive] => [OpenedAt..ClosedAt+1) half-open
        var episodesQ = db.TimeSheets
            .AsNoTracking()
            .Where(t => t.OpenedAt < toExclusive)
            .Where(t => !t.ClosedAt.HasValue || t.ClosedAt.Value.AddDays(1) > fromDate);

        if (mode == RangeMode.ActiveOnFromDate)
        {
            // active on конкретну дату:
            episodesQ = episodesQ
                .Where(t => t.OpenedAt <= fromDate)
                .Where(t => !t.ClosedAt.HasValue || t.ClosedAt.Value >= fromDate);
        }

        var episodes = await episodesQ
            .Select(t => new EpisodeInfo(
                TimesheetId: t.Id,
                PersonId: t.PersonId,
                OpenedAt: t.OpenedAt,
                ClosedAt: t.ClosedAt))
            .ToListAsync(ct);

        if (episodes.Count == 0)
        {
            return new RangeData(
                Codes: codes,
                Persons: [],
                EpisodesByPerson: [],
                EntriesByTimesheet: [],
                TasksByTimesheet: []);
        }

        var personIds = episodes.Select(x => x.PersonId).Distinct().ToArray();

        var personsQ = db.PersonRead
            .AsNoTracking()
            .Where(p => personIds.Contains(p.Id));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            var like = $"%{s}%";
            personsQ = personsQ.Where(p =>
                EF.Functions.Like(p.Rnokpp, like) ||
                EF.Functions.Like(p.FullName, like));
        }

        var persons = await personsQ
            .Select(p => new PersonInfo(
                PersonId: p.Id,
                Rnokpp: p.Rnokpp,
                FullName: p.FullName,
                Rank: p.Rank,
                PositionSort: p.PositionSort,
                Position: p.Position,
                EnrollmentKind: p.EnrollmentKind,
                EnrolledAt: p.EnrolledAt,
                ExcludedAt: p.ExcludedAt))
            .ToListAsync(ct);

        if (persons.Count == 0)
        {
            return new RangeData(
                Codes: codes,
                Persons: [],
                EpisodesByPerson: [],
                EntriesByTimesheet: [],
                TasksByTimesheet: []);
        }

        // Apply search-filtered persons to episodes
        var allowed = persons.Select(x => x.PersonId).ToHashSet();
        episodes = [.. episodes.Where(x => allowed.Contains(x.PersonId))];

        var episodesByPerson = episodes
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.OpenedAt).ToList());

        var tsIds = episodes.Select(x => x.TimesheetId).Distinct().ToArray();

        // Entries segments in range
        var entries = await db.TimesheetEntries
            .AsNoTracking()
            .Where(e => tsIds.Contains(e.TimesheetId))
            .Where(e => !e.IsDeleted)
            .Where(e => e.From < toExclusive && (!e.To.HasValue || e.To.Value > fromDate))
            .OrderBy(e => e.From).ThenBy(e => e.Id)
            .Select(e => new EntrySeg(
                Id: e.Id,
                TimesheetId: e.TimesheetId,
                PersonId: e.PersonId,
                CodeId: e.TimesheetCodeDefinitionId,
                From: e.From,
                ToExclusive: e.To,
                Reference: e.Reference,
                Note: e.Note,
                CreatedAtUtc: e.CreatedAtUtc,
                UpdatedAtUtc: e.UpdatedAtUtc))
            .ToListAsync(ct);

        // Task spans segments in range
        var tasks = await db.TimesheetTaskSpans
            .AsNoTracking()
            .Where(s => tsIds.Contains(s.TimesheetId))
            .Where(s => s.Status != DocumentStatus.Canceled)
            .Where(s => s.FromDate < toExclusive && (!s.ToDate.HasValue || s.ToDate.Value > fromDate))
            .OrderBy(s => s.FromDate).ThenBy(s => s.Id)
            .Select(s => new TaskSeg(
                Id: s.Id,
                TimesheetId: s.TimesheetId,
                PersonId: s.PersonId,
                From: s.FromDate,
                ToExclusive: s.ToDate))
            .ToListAsync(ct);

        return new RangeData(
            Codes: codes,
            Persons: persons,
            EpisodesByPerson: episodesByPerson,
            EntriesByTimesheet: GroupEntries(entries),
            TasksByTimesheet: GroupTasks(tasks));
    }

    //======================================================================
    // Snapshot builder
    //======================================================================

    private static DaySnapshot[] BuildSnapshotsForPerson(
        PersonInfo person,
        RangeData data,
        DateOnly fromDate,
        DateOnly toExclusive,
        bool includeNote)
    {
        var days = toExclusive.DayNumber - fromDate.DayNumber;

        var snaps = new DaySnapshot[days];
        for (var i = 0; i < days; i++)
        {
            snaps[i] = new DaySnapshot(
                CodeId: data.Codes.NbCodeId,
                Code: TimesheetSystemCodes.NotInTimesheet,
                Reference: null,
                Note: null);
        }

        // Episodes for person (could be none)
        if (!data.EpisodesByPerson.TryGetValue(person.PersonId, out var episodes) || episodes.Count == 0)
            return snaps;

        // Apply entries per episode (older -> newer, so newer can override)
        foreach (var ep in episodes)
        {
            if (!data.EntriesByTimesheet.TryGetValue(ep.TimesheetId, out var entrySegs) || entrySegs.Count == 0)
                continue;

            for (var k = 0; k < entrySegs.Count; k++)
            {
                ApplyEntrySegment(snaps, fromDate, toExclusive, data.Codes, entrySegs[k], includeNote);
            }
        }

        // Overlay tasks last (task wins over entry)
        foreach (var ep in episodes)
        {
            if (!data.TasksByTimesheet.TryGetValue(ep.TimesheetId, out var taskSegs) || taskSegs.Count == 0)
                continue;

            for (var k = 0; k < taskSegs.Count; k++)
            {
                ApplyTaskSegment(snaps, fromDate, toExclusive, data.Codes, taskSegs[k]);
            }
        }

        return snaps;
    }

    private static void ApplyEntrySegment(
        DaySnapshot[] snaps,
        DateOnly rangeFrom,
        DateOnly rangeToExclusive,
        CodesMap codes,
        EntrySeg seg,
        bool includeNote)
    {
        var start = Max(rangeFrom, seg.From);
        var end = Min(rangeToExclusive, seg.ToExclusive ?? rangeToExclusive);

        if (end <= start)
            return;

        var code = codes.CodeById(seg.CodeId);
        if (string.IsNullOrWhiteSpace(code))
        {
            // legacy/placeholder => NB
            code = TimesheetSystemCodes.NotInTimesheet;
        }

        var isAlert = IsAlert(code);
        var reference = isAlert && !string.IsNullOrWhiteSpace(seg.Reference) ? seg.Reference.Trim() : null;
        var note = includeNote && !string.IsNullOrWhiteSpace(seg.Note) ? seg.Note.Trim() : null;

        var codeId = string.Equals(code, TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase)
            ? codes.NbCodeId
            : seg.CodeId;

        var startIndex = start.DayNumber - rangeFrom.DayNumber;
        var endIndex = end.DayNumber - rangeFrom.DayNumber;

        for (var i = startIndex; i < endIndex; i++)
        {
            snaps[i] = new DaySnapshot(
                CodeId: codeId,
                Code: code,
                Reference: reference,
                Note: note);
        }
    }

    private static void ApplyTaskSegment(
        DaySnapshot[] snaps,
        DateOnly rangeFrom,
        DateOnly rangeToExclusive,
        CodesMap codes,
        TaskSeg seg)
    {
        var start = Max(rangeFrom, seg.From);
        var end = Min(rangeToExclusive, seg.ToExclusive ?? rangeToExclusive);

        if (end <= start)
            return;

        var startIndex = start.DayNumber - rangeFrom.DayNumber;
        var endIndex = end.DayNumber - rangeFrom.DayNumber;

        for (var i = startIndex; i < endIndex; i++)
        {
            snaps[i] = new DaySnapshot(
                CodeId: codes.TaskCodeId,
                Code: TimesheetSystemCodes.DoesTheCombatTask, // "100"
                Reference: null,
                Note: null);
        }
    }

    //======================================================================
    // Codes map
    //======================================================================

    private static async Task<CodesMap> LoadCodesAsync(AppDbContext db, CancellationToken ct)
    {
        var rows = await db.TimesheetCodes
            .AsNoTracking()
            .Select(c => new CodeRow(c.Id, c.Code))
            .ToListAsync(ct);

        var byId = new Dictionary<Guid, string>(rows.Count);
        var nbId = Guid.Empty;
        var taskId = Guid.Empty;

        foreach (var r in rows)
        {
            var code = (r.Code ?? string.Empty).Trim();
            byId[r.Id] = code;

            if (string.Equals(code, TimesheetSystemCodes.NotInTimesheet, StringComparison.OrdinalIgnoreCase))
                nbId = r.Id;

            if (string.Equals(code, TimesheetSystemCodes.DoesTheCombatTask, StringComparison.OrdinalIgnoreCase))
                taskId = r.Id;
        }

        return new CodesMap(byId, nbId, taskId);
    }

    //======================================================================
    // Helpers / internal models
    //======================================================================

    private static bool IsAlert(string? code)
    {
        var c = (code ?? string.Empty).Trim().ToUpperInvariant();

        // Підтягніть сюди все, що у вас "alert": 100, Ф100, Ф300, тощо.
        return c == TimesheetSystemCodes.DoesTheCombatTask
               || c == TimesheetSystemCodes.InjuryFact;
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a.DayNumber >= b.DayNumber ? a : b;
    private static DateOnly Min(DateOnly a, DateOnly b) => a.DayNumber <= b.DayNumber ? a : b;

    private static Dictionary<Guid, List<EntrySeg>> GroupEntries(List<EntrySeg> entries)
        => entries
            .GroupBy(x => x.TimesheetId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.From).ThenBy(x => x.Id).ToList());

    private static Dictionary<Guid, List<TaskSeg>> GroupTasks(List<TaskSeg> tasks)
        => tasks
            .GroupBy(x => x.TimesheetId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.From).ThenBy(x => x.Id).ToList());

    private sealed record PersonInfo(
        Guid PersonId,
        string Rnokpp,
        string FullName,
        string? Rank,
        int? PositionSort,
        string? Position,
        EnrollmentKind? EnrollmentKind,
        DateOnly? EnrolledAt,
        DateOnly? ExcludedAt);

    private sealed record EpisodeInfo(
        Guid TimesheetId,
        Guid PersonId,
        DateOnly OpenedAt,
        DateOnly? ClosedAt);

    private sealed record EntrySeg(
        Guid Id,
        Guid TimesheetId,
        Guid PersonId,
        Guid CodeId,
        DateOnly From,
        DateOnly? ToExclusive,
        string? Reference,
        string? Note,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);

    private sealed record TaskSeg(
        Guid Id,
        Guid TimesheetId,
        Guid PersonId,
        DateOnly From,
        DateOnly? ToExclusive);

    private sealed record DaySnapshot(
        Guid CodeId,
        string Code,
        string? Reference,
        string? Note);

    private sealed record CodeRow(Guid Id, string? Code);

    private sealed class CodesMap(Dictionary<Guid, string> byId, Guid nbCodeId, Guid taskCodeId)
    {
        public Guid NbCodeId { get; } = nbCodeId;
        public Guid TaskCodeId { get; } = taskCodeId == Guid.Empty ? Guid.Empty : taskCodeId;

        public string CodeById(Guid id)
            => byId.TryGetValue(id, out var code) ? (code ?? string.Empty).Trim() : string.Empty;
    }

    private sealed record RangeData(
        CodesMap Codes,
        List<PersonInfo> Persons,
        Dictionary<Guid, List<EpisodeInfo>> EpisodesByPerson,
        Dictionary<Guid, List<EntrySeg>> EntriesByTimesheet,
        Dictionary<Guid, List<TaskSeg>> TasksByTimesheet);
}
