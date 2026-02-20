//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetViewRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheets;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetViewRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 15, 12, 0, 0, DateTimeKind.Utc);

    //======================================================================
    // GetTimesheetMonthAsync
    //======================================================================

    [Fact]
    public async Task GetTimesheetMonthAsync_builds_month_grid_with_nb_defaults_and_entries()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");
        var cT = NewCode("Т");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, cT);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 10));
            var p3 = NewPerson("333", "Sydorenko Sydir", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01), excludedAt: new DateOnly(2026, 01, 15));
            var p4 = NewPerson("444", "Other Month", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 02, 01));

            db.PersonRead.AddRange(p1, p2, p3, p4);

            // IMPORTANT: SQLite не підтримує filtered unique index, тому в тестах — 1 епізод на personId.
            var ts1 = NewEpisode(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            var ts2 = NewEpisode(p2.Id, openedAt: new DateOnly(2026, 01, 10));
            var ts3 = NewEpisode(p3.Id, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 01, 15));
            var ts4 = NewEpisode(p4.Id, openedAt: new DateOnly(2026, 02, 01));

            db.TimeSheets.AddRange(ts1, ts2, ts3, ts4);

            db.TimesheetEntries.AddRange(
                NewEntry(ts1, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), toExclusive: null),
                NewEntry(ts2, p2.Id, c30.Id, from: new DateOnly(2026, 01, 10), toExclusive: null),

                // half-open: 01..15 (inclusive) => ToExclusive = 16
                NewEntry(ts3, p3.Id, c30.Id, from: new DateOnly(2026, 01, 01), toExclusive: new DateOnly(2026, 01, 16)),

                // у лютому — не має потрапити в січневий grid
                NewEntry(ts4, p4.Id, cT.Id, from: new DateOnly(2026, 02, 01), toExclusive: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetViewRepository(tdb.Factory);

        var rows = await repo.GetTimesheetMonthAsync(2026, 1, search: null);

        var daysInMonth = DateTime.DaysInMonth(2026, 1);
        Assert.Equal(3, rows.Count);

        Assert.All(rows, r =>
        {
            Assert.Equal(daysInMonth, r.Codes.Count);
            Assert.Equal(daysInMonth, r.Referenses.Count);
        });

        // p3: 01..15 => "30", 16..31 => "НБ"
        var r3 = rows.Single(r => r.Rnokpp == "333");
        Assert.All(r3.Codes.Take(15), c => Assert.Equal("30", c));
        Assert.All(r3.Codes.Skip(15), c => Assert.Equal(TimesheetSystemCodes.NotInTimesheet, c));

        // p2: 01..09 => "НБ", 10..31 => "30"
        var r2 = rows.Single(r => r.Rnokpp == "222");
        Assert.All(r2.Codes.Take(9), c => Assert.Equal(TimesheetSystemCodes.NotInTimesheet, c));
        Assert.All(r2.Codes.Skip(9), c => Assert.Equal("30", c));

        // p4 не перетинає січень
        Assert.DoesNotContain(rows, r => r.Rnokpp == "444");
    }

    [Fact]
    public async Task GetTimesheetMonthAsync_sets_reference_only_for_alert_codes_100_and_f100()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");
        var c100 = NewCode("100");   // alert
        var cF100 = NewCode("Ф100"); // alert

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, c100, cF100);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01));
            db.PersonRead.Add(p1);

            var ts = NewEpisode(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            db.TimeSheets.Add(ts);

            db.TimesheetEntries.AddRange(
                // 01..04 (inclusive) => ToExclusive = 05
                NewEntry(ts, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), toExclusive: new DateOnly(2026, 01, 05), reference: "REF-NON-ALERT"),

                // day 05 only => ToExclusive = 06
                NewEntry(ts, p1.Id, c100.Id, from: new DateOnly(2026, 01, 05), toExclusive: new DateOnly(2026, 01, 06), reference: " REF-ABC "),

                // day 07 only => ToExclusive = 08
                NewEntry(ts, p1.Id, cF100.Id, from: new DateOnly(2026, 01, 07), toExclusive: new DateOnly(2026, 01, 08), reference: " REF-F100 "),

                NewEntry(ts, p1.Id, c30.Id, from: new DateOnly(2026, 01, 08), toExclusive: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetViewRepository(tdb.Factory);

        var rows = await repo.GetTimesheetMonthAsync(2026, 1, search: "111");
        Assert.Single(rows);

        var r = rows[0];

        // day 5 => index 4
        Assert.Equal("100", r.Codes[4]);
        Assert.Equal("REF-ABC", r.Referenses[4]);

        // day 7 => index 6
        Assert.Equal("Ф100", r.Codes[6]);
        Assert.Equal("REF-F100", r.Referenses[6]);

        // non-alert days => no reference
        Assert.Null(r.Referenses[0]);
        Assert.Null(r.Referenses[3]);
        Assert.Null(r.Referenses[7]); // day 8
    }

    [Fact]
    public async Task GetTimesheetMonthAsync_applies_search_by_rnokpp_or_fullname()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01));

            db.PersonRead.AddRange(p1, p2);

            var ts1 = NewEpisode(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            var ts2 = NewEpisode(p2.Id, openedAt: new DateOnly(2026, 01, 01));
            db.TimeSheets.AddRange(ts1, ts2);

            db.TimesheetEntries.AddRange(
                NewEntry(ts1, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), toExclusive: null),
                NewEntry(ts2, p2.Id, c30.Id, from: new DateOnly(2026, 01, 01), toExclusive: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetViewRepository(tdb.Factory);

        var byRnokpp = await repo.GetTimesheetMonthAsync(2026, 1, "111");
        Assert.Single(byRnokpp);
        Assert.Equal("111", byRnokpp[0].Rnokpp);

        var byName = await repo.GetTimesheetMonthAsync(2026, 1, "petrenko");
        Assert.Single(byName);
        Assert.Equal("222", byName[0].Rnokpp);
    }

    //======================================================================
    // GetTimesheetRangeAsync
    //======================================================================

    [Fact]
    public async Task GetTimesheetRangeAsync_maps_blank_code_to_nb_code_and_nb_codeId_and_sets_alert_reference_only()
    {
        await using var tdb = new SqliteTestDb();

        var cNB = NewCode(TimesheetSystemCodes.NotInTimesheet); // required (SingleAsync)
        var cBlank = NewCode(""); // "task placeholder" (should map to NB in range DTO)
        var c100 = NewCode("100"); // alert
        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(cNB, cBlank, c100, c30);

            var p = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 02, 01));
            db.PersonRead.Add(p);

            var ts = NewEpisode(p.Id, openedAt: new DateOnly(2026, 02, 01));
            db.TimeSheets.Add(ts);

            db.TimesheetEntries.AddRange(
                // 01..02 (inclusive) => ToExclusive = 03
                NewEntry(ts, p.Id, cBlank.Id, from: new DateOnly(2026, 02, 01), toExclusive: new DateOnly(2026, 02, 03), reference: "SHOULD-NOT-APPEAR"),

                // day 03 only => ToExclusive = 04
                NewEntry(ts, p.Id, c100.Id, from: new DateOnly(2026, 02, 03), toExclusive: new DateOnly(2026, 02, 04), reference: " REF-ALERT "),

                NewEntry(ts, p.Id, c30.Id, from: new DateOnly(2026, 02, 04), toExclusive: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetViewRepository(tdb.Factory);

        var from = new DateOnly(2026, 02, 01);
        var to = new DateOnly(2026, 02, 05);

        var rows = await repo.GetTimesheetRangeAsync(from, to, search: "111");
        Assert.Single(rows);

        var r = rows[0];
        Assert.Equal(5, r.Days.Count);

        // 01..02 => blank code in DB => NB in DTO + NB id
        Assert.Equal(new DateOnly(2026, 02, 01), r.Days[0].Date);
        Assert.Equal(TimesheetSystemCodes.NotInTimesheet, r.Days[0].Code);
        Assert.Equal(GetNbId(rows), r.Days[0].CodeId);
        Assert.Null(r.Days[0].Reference);

        Assert.Equal(new DateOnly(2026, 02, 02), r.Days[1].Date);
        Assert.Equal(TimesheetSystemCodes.NotInTimesheet, r.Days[1].Code);
        Assert.Equal(GetNbId(rows), r.Days[1].CodeId);
        Assert.Null(r.Days[1].Reference);

        // 03 => alert => reference included
        Assert.Equal(new DateOnly(2026, 02, 03), r.Days[2].Date);
        Assert.Equal("100", r.Days[2].Code);
        Assert.NotEqual(GetNbId(rows), r.Days[2].CodeId);
        Assert.Equal("REF-ALERT", r.Days[2].Reference);

        // 04..05 => 30 => no reference
        Assert.Equal("30", r.Days[3].Code);
        Assert.Null(r.Days[3].Reference);

        Assert.Equal("30", r.Days[4].Code);
        Assert.Null(r.Days[4].Reference);
    }

    private static Guid GetNbId(IReadOnlyList<TimesheetPersonRangeRowDto> rows)
    {
        // NB id буде однаковим у всіх рядках; беремо з першого дня першого рядка
        var r0 = rows[0];
        var nb = r0.Days.First(d => d.Code == TimesheetSystemCodes.NotInTimesheet);
        return nb.CodeId;
    }

    //======================================================================
    // GetTimesheetPersonMonthAsync
    //======================================================================

    [Fact]
    public async Task GetTimesheetPersonMonthAsync_returns_null_when_person_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetViewRepository(tdb.Factory);

        var res = await repo.GetTimesheetPersonMonthAsync(Guid.NewGuid(), 2026, 2);
        Assert.Null(res);
    }

    [Fact]
    public async Task GetTimesheetPersonMonthAsync_builds_codes_with_gaps_and_returns_entries_and_updatedAtUtc()
    {
        await using var tdb = new SqliteTestDb();

        var cNB = NewCode(TimesheetSystemCodes.NotInTimesheet);
        var cT = NewCode("Т");

        Guid personId;

        var tEntryUpdated = NowUtc.AddHours(1);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(cNB, cT);

            var p = NewPerson("444", "Episode Person", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 02, 28));
            personId = p.Id;
            db.PersonRead.Add(p);

            var ts = NewEpisode(p.Id, openedAt: new DateOnly(2026, 02, 01));
            db.TimeSheets.Add(ts);

            db.TimesheetEntries.AddRange(
                // 02..07 (inclusive) => ToExclusive = 08
                NewEntry(ts, p.Id, cT.Id, from: new DateOnly(2026, 02, 02), toExclusive: new DateOnly(2026, 02, 08),
                    reference: " R1 ", note: " N1 ",
                    createdAtUtc: NowUtc, updatedAtUtc: tEntryUpdated),

                NewEntry(ts, p.Id, cT.Id, from: new DateOnly(2026, 02, 28), toExclusive: null,
                    createdAtUtc: NowUtc.AddMinutes(10), updatedAtUtc: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetViewRepository(tdb.Factory);

        var pm = await repo.GetTimesheetPersonMonthAsync(personId, 2026, 2);
        Assert.NotNull(pm);

        Assert.Equal(2026, pm!.Year);
        Assert.Equal(2, pm.Month);
        Assert.Equal(28, pm.DaysInMonth);

        // UpdatedAtUtc = max(UpdatedAtUtc ?? CreatedAtUtc)
        Assert.Equal(tEntryUpdated, pm.UpdatedAtUtc);

        // Entries list (To is EXCLUSIVE in DB/DTO)
        Assert.Equal(2, pm.Entries.Count);
        Assert.Contains(pm.Entries, e => e.Code == "Т" && e.From == new DateOnly(2026, 02, 02) && e.To == new DateOnly(2026, 02, 08));
        Assert.Contains(pm.Entries, e => e.Code == "Т" && e.From == new DateOnly(2026, 02, 28) && e.To is null);

        // Day codes: 01 => NB; 02..07 => Т; 08..27 => NB; 28 => Т
        var codes = pm.Person.Codes;
        Assert.Equal(28, codes.Count);

        Assert.Equal(TimesheetSystemCodes.NotInTimesheet, codes[0]);

        for (var i = 1; i <= 6; i++)
            Assert.Equal("Т", codes[i]);

        for (var i = 7; i <= 26; i++)
            Assert.Equal(TimesheetSystemCodes.NotInTimesheet, codes[i]);

        Assert.Equal("Т", codes[27]);
    }

    //======================================================================
    // GetTimesheetDayAsync
    //======================================================================

    [Fact]
    public async Task GetTimesheetDayAsync_returns_only_persons_in_timesheet_on_date_and_picks_latest_entry()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");
        var c100 = NewCode("100");

        var date = new DateOnly(2026, 01, 12);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, c100);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 10));
            var p3 = NewPerson("333", "Sydorenko Sydir", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01), excludedAt: new DateOnly(2026, 01, 11));

            db.PersonRead.AddRange(p1, p2, p3);

            // episodes
            var ts1 = NewEpisode(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            var ts2 = NewEpisode(p2.Id, openedAt: new DateOnly(2026, 01, 10));
            var ts3 = NewEpisode(p3.Id, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 01, 11)); // closed before date

            db.TimeSheets.AddRange(ts1, ts2, ts3);

            // p1: older open-ended + newer open-ended => newer should win
            db.TimesheetEntries.AddRange(
                NewEntry(ts1, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), toExclusive: null, reference: "OLD", note: "OLDN"),
                NewEntry(ts1, p1.Id, c100.Id, from: new DateOnly(2026, 01, 11), toExclusive: null, reference: " NEW-REF ", note: " NEW-NOTE ")
            );

            // p2: no entries => defaults
            db.SaveChanges();
        }

        var repo = new TimesheetViewRepository(tdb.Factory);

        var rows = await repo.GetTimesheetDayAsync(date, search: null);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.Rnokpp == "111");
        Assert.Contains(rows, r => r.Rnokpp == "222");
        Assert.DoesNotContain(rows, r => r.Rnokpp == "333");

        var r1 = rows.Single(r => r.Rnokpp == "111");
        Assert.Equal("100", r1.DayState.Code);
        Assert.Equal("NEW-REF", r1.DayState.Reference);
        Assert.Equal("NEW-NOTE", r1.DayState.Note);

        var r2 = rows.Single(r => r.Rnokpp == "222");
        Assert.Equal(Guid.Empty, r2.DayState.CodeId);
        Assert.Equal(TimesheetSystemCodes.NotInTimesheet, r2.DayState.Code);
        Assert.Null(r2.DayState.Reference);
        Assert.Null(r2.DayState.Note);
    }

    [Fact]
    public async Task GetTimesheetDayAsync_applies_search_by_rnokpp_or_fullname()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");
        var date = new DateOnly(2026, 01, 12);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 01, 01));

            db.PersonRead.AddRange(p1, p2);

            var ts1 = NewEpisode(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            var ts2 = NewEpisode(p2.Id, openedAt: new DateOnly(2026, 01, 01));
            db.TimeSheets.AddRange(ts1, ts2);

            db.TimesheetEntries.AddRange(
                NewEntry(ts1, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), toExclusive: null),
                NewEntry(ts2, p2.Id, c30.Id, from: new DateOnly(2026, 01, 01), toExclusive: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetViewRepository(tdb.Factory);

        var byRnokpp = await repo.GetTimesheetDayAsync(date, "111");
        Assert.Single(byRnokpp);
        Assert.Equal("111", byRnokpp[0].Rnokpp);

        var byName = await repo.GetTimesheetDayAsync(date, "petrenko");
        Assert.Single(byName);
        Assert.Equal("222", byName[0].Rnokpp);
    }

    //======================================================================
    // Test data helpers
    //======================================================================

    private static PersonReadModel NewPerson(
        string rnokpp,
        string fullName,
        EnrollmentKind kind,
        DateOnly enrolledAt,
        DateOnly? excludedAt = null)
    {
        var parts = (fullName ?? "X Y").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var last = parts.Length > 0 ? parts[0] : "X";
        var first = parts.Length > 1 ? parts[1] : "Y";

        return new PersonReadModel
        {
            Id = Guid.NewGuid(),
            Rnokpp = rnokpp,
            FullName = fullName ?? string.Empty,
            LastName = last,
            FirstName = first,
            MiddleName = null,

            Lifecycle = excludedAt is null ? PersonLifecycle.Enrolled : PersonLifecycle.Reserved,

            EnrollmentKind = kind,
            EnrollmentReference = null,

            Rank = null,
            PositionSort = 0,
            Position = null,

            Bzvp = null,
            Weapon = null,
            Callsign = null,

            EnrolledAt = enrolledAt,
            ExcludedAt = excludedAt,

            Version = 1,
            UpdatedAtUtc = NowUtc
        };
    }

    private static TimeSheetAggregate NewEpisode(Guid personId, DateOnly openedAt, DateOnly? closedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = "tester",
            CreatedAtUtc = NowUtc
        };

    private static TimesheetCodeDefinition NewCode(
        string code,
        string? title = null,
        int sortOrder = 0,
        int priority = 0,
        bool isTerminal = false,
        bool isActive = true)
        => new()
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = title ?? (code.Length == 0 ? "<empty>" : code),
            Description = null,
            SortOrder = sortOrder,
            Priority = priority,
            IsTerminal = isTerminal,
            IsActive = isActive,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc
        };

    private static TimesheetEntry NewEntry(
        TimeSheetAggregate episode,
        Guid personId,
        Guid codeId,
        DateOnly from,
        DateOnly? toExclusive,
        string? reference = null,
        string? note = null,
        bool isDeleted = false,
        DateTime? createdAtUtc = null,
        DateTime? updatedAtUtc = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TimesheetId = episode.Id,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,

            // TimesheetEntry is half-open: [From..To) where To is exclusive
            From = from,
            To = toExclusive,

            Reference = reference,
            Note = note,
            CreatedBy = "tester",
            CreatedAtUtc = createdAtUtc ?? NowUtc,
            UpdatedBy = updatedAtUtc.HasValue ? "tester" : null,
            UpdatedAtUtc = updatedAtUtc,
            IsDeleted = isDeleted
        };
}
