//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMonthRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetMonthRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 23, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetTimesheetMonthAsync_builds_month_grid_with_nb_defaults_and_entries()
    {
        await using var tdb = new SqliteTestDb();

        // seed codes
        var c30 = NewCode("30");
        var cT = NewCode("Т");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, cT);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, new DateOnly(2026, 01, 10));
            var p3 = NewPerson("333", "Sydorenko Sydir", EnrollmentKind.Unit, new DateOnly(2026, 01, 01),
                excludedAt: new DateOnly(2026, 01, 15));

            db.PersonRead.AddRange(p1, p2, p3);

            var t1 = NewTimeline(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            var t2 = NewTimeline(p2.Id, openedAt: new DateOnly(2026, 01, 10));
            var t3 = NewTimeline(p3.Id, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 01, 15));

            db.TimesheetTimelines.AddRange(t1, t2, t3);

            db.TimesheetEntries.AddRange(
                NewEntry(t1, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), to: null),
                NewEntry(t2, p2.Id, c30.Id, from: new DateOnly(2026, 01, 10), to: null),
                NewEntry(t3, p3.Id, c30.Id, from: new DateOnly(2026, 01, 01), to: new DateOnly(2026, 01, 15))
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        // act
        var rows = await repo.GetTimesheetMonthAsync(2026, 1, search: null);

        // assert
        var daysInMonth = DateTime.DaysInMonth(2026, 1);

        Assert.Equal(3, rows.Count);

        Assert.All(rows, r =>
        {
            Assert.Equal(daysInMonth, r.Codes.Count);
            Assert.Equal(daysInMonth, r.Referenses.Count);
        });

        // p3: 1..15 => "30", 16..31 => "НБ"
        var r3 = rows.Single(r => r.RNOKPP == "333");
        Assert.All(r3.Codes.Take(15), c => Assert.Equal("30", c));
        Assert.All(r3.Codes.Skip(15), c => Assert.Equal("НБ", c));

        // p2: 1..9 => "НБ", 10..31 => "30"
        var r2 = rows.Single(r => r.RNOKPP == "222");
        Assert.All(r2.Codes.Take(9), c => Assert.Equal("НБ", c));
        Assert.All(r2.Codes.Skip(9), c => Assert.Equal("30", c));
    }

    [Fact]
    public async Task GetTimesheetMonthAsync_applies_search_by_rnokpp_or_fullname()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));

            db.PersonRead.AddRange(p1, p2);

            var t1 = NewTimeline(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            var t2 = NewTimeline(p2.Id, openedAt: new DateOnly(2026, 01, 01));
            db.TimesheetTimelines.AddRange(t1, t2);

            db.TimesheetEntries.AddRange(
                NewEntry(t1, p1.Id, c30.Id, new DateOnly(2026, 01, 01), null),
                NewEntry(t2, p2.Id, c30.Id, new DateOnly(2026, 01, 01), null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);
        var daysInMonth = DateTime.DaysInMonth(2026, 1);

        var byRnokpp = await repo.GetTimesheetMonthAsync(2026, 1, "111");
        Assert.Single(byRnokpp);
        Assert.Equal("111", byRnokpp[0].RNOKPP);
        Assert.Equal(daysInMonth, byRnokpp[0].Referenses.Count);

        var byName = await repo.GetTimesheetMonthAsync(2026, 1, "petrenko");
        Assert.Single(byName);
        Assert.Equal("222", byName[0].RNOKPP);
        Assert.Equal(daysInMonth, byName[0].Referenses.Count);
    }

    [Fact]
    public async Task GetTimesheetMonthAsync_sets_MainRef100_only_for_alert_codes()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");
        var c100 = NewCode("100"); // alert
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, c100);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            db.PersonRead.Add(p1);

            var t1 = NewTimeline(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            db.TimesheetTimelines.Add(t1);

            db.TimesheetEntries.AddRange(
                NewEntry(t1, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), to: new DateOnly(2026, 01, 04)),
                NewEntry(t1, p1.Id, c100.Id, from: new DateOnly(2026, 01, 05), to: new DateOnly(2026, 01, 05), reference: "REF-ABC"),
                NewEntry(t1, p1.Id, c30.Id, from: new DateOnly(2026, 01, 06), to: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        var rows = await repo.GetTimesheetMonthAsync(2026, 1, search: "111");
        Assert.Single(rows);

        var r = rows[0];

        // day 5 => index 4
        Assert.Equal("100", r.Codes[4]);
        Assert.Equal("REF-ABC", r.Referenses[4]);

        Assert.True(string.IsNullOrWhiteSpace(r.Referenses[3]));
        Assert.True(string.IsNullOrWhiteSpace(r.Referenses[5]));
    }

    [Fact]
    public async Task GetTimesheetDayAsync_returns_only_persons_whose_timeline_overlaps_date_and_applies_defaults()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");

        var date = new DateOnly(2026, 01, 12);

        Guid p1Id, p2Id, p3Id;

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(c30);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, new DateOnly(2026, 01, 10));
            var p3 = NewPerson("333", "Sydorenko Sydir", EnrollmentKind.Unit, new DateOnly(2026, 01, 01),
                excludedAt: new DateOnly(2026, 01, 11));

            p1Id = p1.Id; p2Id = p2.Id; p3Id = p3.Id;

            db.PersonRead.AddRange(p1, p2, p3);

            // timelines (p3 closed before date => excluded)
            var t1 = NewTimeline(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            var t2 = NewTimeline(p2.Id, openedAt: new DateOnly(2026, 01, 10));
            var t3 = NewTimeline(p3.Id, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 01, 11));

            db.TimesheetTimelines.AddRange(t1, t2, t3);

            // only p1 has an entry, p2 has none => defaults
            db.TimesheetEntries.Add(
                NewEntry(t1, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), to: null, reference: " R-MAIN ", note: " N-MAIN ")
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        var rows = await repo.GetTimesheetDayAsync(date, search: null);

        Assert.Equal(2, rows.Count);

        var r1 = rows.Single(r => r.RNOKPP == "111");
        Assert.Equal(p1Id, r1.PersonId);
        Assert.Equal(c30.Id, r1.DayState.CodeId);
        Assert.Equal("30", r1.DayState.Code);
        Assert.Equal("R-MAIN", r1.DayState.Reference);
        Assert.Equal("N-MAIN", r1.DayState.Note);

        var r2 = rows.Single(r => r.RNOKPP == "222");
        Assert.Equal(p2Id, r2.PersonId);
        Assert.Equal(Guid.Empty, r2.DayState.CodeId);
        Assert.Equal("НБ", r2.DayState.Code);
        Assert.Null(r2.DayState.Reference);
        Assert.Null(r2.DayState.Note);

        Assert.DoesNotContain(rows, r => r.PersonId == p3Id);
    }

    [Fact]
    public async Task GetTimesheetDayAsync_picks_latest_active_entry_per_person_on_date()
    {
        await using var tdb = new SqliteTestDb();

        var c30 = NewCode("30");
        var c100 = NewCode("100");

        var date = new DateOnly(2026, 01, 12);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c30, c100);

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            db.PersonRead.Add(p1);

            var t = NewTimeline(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            db.TimesheetTimelines.Add(t);

            // older open-ended + newer open-ended (newer should win)
            db.TimesheetEntries.AddRange(
                NewEntry(t, p1.Id, c30.Id, from: new DateOnly(2026, 01, 01), to: null, reference: "OLD", note: "OLDN"),
                NewEntry(t, p1.Id, c100.Id, from: new DateOnly(2026, 01, 11), to: null, reference: " NEW-REF ", note: " NEW-NOTE ")
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        var rows = await repo.GetTimesheetDayAsync(date, search: null);

        Assert.Single(rows);

        var r = rows[0];

        Assert.Equal(c100.Id, r.DayState.CodeId);
        Assert.Equal("100", r.DayState.Code);
        Assert.Equal("NEW-REF", r.DayState.Reference);
        Assert.Equal("NEW-NOTE", r.DayState.Note);
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

            var p1 = NewPerson("111", "Ivanov Ivan", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));
            var p2 = NewPerson("222", "Petrenko Petro", EnrollmentKind.Unit, new DateOnly(2026, 01, 01));

            db.PersonRead.AddRange(p1, p2);

            var t1 = NewTimeline(p1.Id, openedAt: new DateOnly(2026, 01, 01));
            var t2 = NewTimeline(p2.Id, openedAt: new DateOnly(2026, 01, 01));
            db.TimesheetTimelines.AddRange(t1, t2);

            db.TimesheetEntries.AddRange(
                NewEntry(t1, p1.Id, c30.Id, new DateOnly(2026, 01, 01), null),
                NewEntry(t2, p2.Id, c30.Id, new DateOnly(2026, 01, 01), null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        var byRnokpp = await repo.GetTimesheetDayAsync(date, "111");
        Assert.Single(byRnokpp);
        Assert.Equal("111", byRnokpp[0].RNOKPP);

        var byName = await repo.GetTimesheetDayAsync(date, "petrenko");
        Assert.Single(byName);
        Assert.Equal("222", byName[0].RNOKPP);
    }

    [Fact]
    public async Task GetTimesheetMonthAsync_includes_multiple_episodes_in_month_and_keeps_gaps_as_NB()
    {
        await using var tdb = new SqliteTestDb();

        var cT = NewCode("Т");

        Guid personId;

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(cT);

            // PersonRead потрібен лише щоб репо побудувало row.
            var p = NewPerson("444", "Episode Person", EnrollmentKind.Unit, enrolledAt: new DateOnly(2026, 02, 28));
            personId = p.Id;
            db.PersonRead.Add(p);

            // Епізод 1: 02.02–07.02
            var tl1 = NewTimeline(personId, openedAt: new DateOnly(2026, 02, 02), closedAt: new DateOnly(2026, 02, 07));

            // Епізод 2: 28.02–(open)
            var tl2 = NewTimeline(personId, openedAt: new DateOnly(2026, 02, 28), closedAt: null);

            db.TimesheetTimelines.AddRange(tl1, tl2);

            // Entry-сегменти в межах кожного епізоду
            db.TimesheetEntries.AddRange(
                NewEntry(tl1, personId, cT.Id, from: new DateOnly(2026, 02, 02), to: new DateOnly(2026, 02, 07)),
                NewEntry(tl2, personId, cT.Id, from: new DateOnly(2026, 02, 28), to: null)
            );

            db.SaveChanges();
        }

        var repo = new TimesheetMonthRepository(tdb.Factory);

        // act (grid)
        var rows = await repo.GetTimesheetMonthAsync(2026, 2, search: "444");

        // assert
        Assert.Single(rows);
        var r = rows[0];

        Assert.Equal(28, r.Codes.Count);

        // 01.02 => NB
        Assert.Equal("НБ", r.Codes[0]);

        // 02..07 => "Т" (indexes 1..6)
        for (var i = 1; i <= 6; i++)
            Assert.Equal("Т", r.Codes[i]);

        // 08..27 => NB (indexes 7..26)
        for (var i = 7; i <= 26; i++)
            Assert.Equal("НБ", r.Codes[i]);

        // 28 => "Т" (index 27)
        Assert.Equal("Т", r.Codes[27]);

        // додатково: person-month details також має містити обидва сегменти
        var pm = await repo.GetTimesheetPersonMonthAsync(personId, 2026, 2);
        Assert.NotNull(pm);
        Assert.Equal(2, pm!.Entries.Count);

        Assert.Contains(pm.Entries, e => e.Code == "Т" && e.From == new DateOnly(2026, 02, 02) && e.To == new DateOnly(2026, 02, 07));
        Assert.Contains(pm.Entries, e => e.Code == "Т" && e.From == new DateOnly(2026, 02, 28) && e.To is null);
    }

    // -------------------------
    // Test data helpers
    // -------------------------

    private static PersonReadModel NewPerson(
        string rnokpp,
        string fullName,
        EnrollmentKind kind,
        DateOnly enrolledAt,
        DateOnly? excludedAt = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Rnokpp = rnokpp,
            FullName = fullName,
            LastName = fullName.Split(' ')[0],
            FirstName = fullName.Split(' ').Length > 1 ? fullName.Split(' ')[1] : "X",
            Lifecycle = excludedAt is null ? PersonLifecycle.Enrolled : PersonLifecycle.Reserved,
            EnrollmentKind = kind,
            EnrolledAt = enrolledAt,
            ExcludedAt = excludedAt,
            PositionSort = 0,
            Version = 1,
            UpdatedAtUtc = NowUtc
        };

    private static TimesheetTimeline NewTimeline(
        Guid personId,
        DateOnly openedAt,
        DateOnly? closedAt = null)
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
            Title = title ?? code,
            Description = null,
            SortOrder = sortOrder,
            Priority = priority,
            IsTerminal = isTerminal,
            IsActive = isActive,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc
        };

    private static TimesheetEntry NewEntry(
        TimesheetTimeline timeline,
        Guid personId,
        Guid codeId,
        DateOnly from,
        DateOnly? to,
        string? reference = null,
        string? note = null)
        => new()
        {
            Id = Guid.NewGuid(),
            TimelineId = timeline.Id,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = from,
            To = to,
            Reference = reference,
            Note = note,
            CreatedBy = "tester",
            CreatedAtUtc = NowUtc,
            IsDeleted = false
        };
}
