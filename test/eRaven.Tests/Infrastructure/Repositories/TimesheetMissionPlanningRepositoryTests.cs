//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMissionPlanningRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetMissionPlanningRepositoryTests
{
    //======================================================================
    // GetActiveMissionPersonsAsync
    //======================================================================

    [Fact]
    public async Task GetActiveMissionPersonsAsync_ReturnsOnlyActiveNonCanceled_HalfOpen()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var onDate = new DateOnly(2026, 02, 10);
        var missionId = Guid.NewGuid();

        var pA = Guid.NewGuid();
        var pB = Guid.NewGuid();
        var pC = Guid.NewGuid();
        var pD = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();

            var tsA = SeedEpisode(db, pA, openedAt: new DateOnly(2026, 02, 01), closedAt: null, nowUtc: now);
            var tsB = SeedEpisode(db, pB, new DateOnly(2026, 02, 01), null, now);
            var tsC = SeedEpisode(db, pC, new DateOnly(2026, 02, 01), null, now);
            var tsD = SeedEpisode(db, pD, new DateOnly(2026, 02, 01), null, now);

            // A -> active (open-ended)
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: tsA.Id,
                personId: pA,
                missionId: missionId,
                fullName: "Alpha A",
                from: new DateOnly(2026, 02, 01),
                toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: Guid.NewGuid(),
                closedByDocId: null,
                closedByCodeId: null,
                nowUtc: now));

            // B -> ToDate == onDate (exclusive) => NOT active on onDate
            db.TimesheetTaskSpans.Add(NewSpan(
                tsB.Id, pB, missionId, "Bravo B",
                from: new DateOnly(2026, 02, 05),
                toExclusive: onDate,
                status: DocumentStatus.Active,
                openedByDocId: Guid.NewGuid(),
                closedByDocId: null,
                closedByCodeId: null,
                nowUtc: now));

            // C -> canceled => excluded
            db.TimesheetTaskSpans.Add(NewSpan(
                tsC.Id, pC, missionId, "Charlie C",
                from: new DateOnly(2026, 02, 09),
                toExclusive: new DateOnly(2026, 02, 12),
                status: DocumentStatus.Canceled,
                openedByDocId: Guid.NewGuid(),
                closedByDocId: null,
                closedByCodeId: Guid.NewGuid(),
                nowUtc: now));

            // D -> different mission => excluded
            db.TimesheetTaskSpans.Add(NewSpan(
                tsD.Id, pD, Guid.NewGuid(), "Delta D",
                from: new DateOnly(2026, 02, 01),
                toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: Guid.NewGuid(),
                closedByDocId: null,
                closedByCodeId: null,
                nowUtc: now));

            await db.SaveChangesAsync();
        }

        var result = await repo.GetActiveMissionPersonsAsync(missionId, onDate);

        Assert.Single(result);
        Assert.Equal(pA, result[0].PersonId);
        Assert.Equal("Alpha A", result[0].FullName);
        Assert.Equal(new DateOnly(2026, 02, 01), result[0].FromDate);
    }

    [Fact]
    public async Task GetActiveMissionPersonsAsync_OrdersByFullName_ThenByFrom()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var onDate = new DateOnly(2026, 02, 10);
        var missionId = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();

            var ts1 = SeedEpisode(db, p1, new DateOnly(2026, 02, 01), null, now);
            var ts2 = SeedEpisode(db, p2, new DateOnly(2026, 02, 01), null, now);

            // same FullName => order by FromDate
            db.TimesheetTaskSpans.Add(NewSpan(ts1.Id, p1, missionId, "Alpha",
                from: new DateOnly(2026, 02, 05), toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: Guid.NewGuid(), closedByDocId: null, closedByCodeId: null, nowUtc: now));

            db.TimesheetTaskSpans.Add(NewSpan(ts2.Id, p2, missionId, "Alpha",
                from: new DateOnly(2026, 02, 03), toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: Guid.NewGuid(), closedByDocId: null, closedByCodeId: null, nowUtc: now));

            await db.SaveChangesAsync();
        }

        var result = await repo.GetActiveMissionPersonsAsync(missionId, onDate);

        Assert.Equal(2, result.Count);
        Assert.Equal(p2, result[0].PersonId); // From 02-03
        Assert.Equal(p1, result[1].PersonId); // From 02-05
    }

    //======================================================================
    // GetActiveMissionPersonsByDocumentAsync
    //======================================================================

    [Fact]
    public async Task GetActiveMissionPersonsByDocumentAsync_ReturnsOpenedOrClosedByDocument()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var onDate = new DateOnly(2026, 02, 10);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var pA = Guid.NewGuid();
        var pB = Guid.NewGuid();
        var pC = Guid.NewGuid();
        var pD = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();

            var tsA = SeedEpisode(db, pA, new DateOnly(2026, 02, 01), null, now);
            var tsB = SeedEpisode(db, pB, new DateOnly(2026, 02, 01), null, now);
            var tsC = SeedEpisode(db, pC, new DateOnly(2026, 02, 01), null, now);
            var tsD = SeedEpisode(db, pD, new DateOnly(2026, 02, 01), null, now);

            // A: openedBy == doc (active)
            db.TimesheetTaskSpans.Add(NewSpan(
                tsA.Id, pA, missionId, "Alpha Doc",
                from: new DateOnly(2026, 02, 05), toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: documentId, closedByDocId: null, closedByCodeId: null, nowUtc: now));

            // B: closedBy == doc (active on onDate; ToDate > onDate)
            db.TimesheetTaskSpans.Add(NewSpan(
                tsB.Id, pB, missionId, "Bravo Doc",
                from: new DateOnly(2026, 02, 01), toExclusive: new DateOnly(2026, 02, 11),
                status: DocumentStatus.Active,
                openedByDocId: Guid.NewGuid(), closedByDocId: documentId, closedByCodeId: null, nowUtc: now));

            // C: unrelated doc => excluded
            db.TimesheetTaskSpans.Add(NewSpan(
                tsC.Id, pC, missionId, "Charlie Other",
                from: new DateOnly(2026, 02, 01), toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: Guid.NewGuid(), closedByDocId: null, closedByCodeId: null, nowUtc: now));

            // D: half-open boundary => excluded (ToDate == onDate)
            db.TimesheetTaskSpans.Add(NewSpan(
                tsD.Id, pD, missionId, "Delta Boundary",
                from: new DateOnly(2026, 02, 01), toExclusive: onDate,
                status: DocumentStatus.Active,
                openedByDocId: documentId, closedByDocId: null, closedByCodeId: null, nowUtc: now));

            await db.SaveChangesAsync();
        }

        var result = await repo.GetActiveMissionPersonsByDocumentAsync(documentId, onDate);

        Assert.Equal(2, result.Count);
        Assert.Equal("Alpha Doc", result[0].FullName);
        Assert.Equal("Bravo Doc", result[1].FullName);
    }

    //======================================================================
    // GetActiveMissionClosablePersonsAsync
    //======================================================================

    [Fact]
    public async Task GetActiveMissionClosablePersonsAsync_ReturnsOnlyOpenActiveNotFinalizedSpans()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var missionId = Guid.NewGuid();
        var otherMissionId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 02, 20);
        var openedAt = new DateOnly(2026, 02, 01);
        var now = Utc(2026, 02, 18, 10, 00);

        var p1 = Guid.NewGuid(); // ✅ open + active + not finalized -> returned
        var p2 = Guid.NewGuid(); // closed by document -> excluded
        var p3 = Guid.NewGuid(); // closed by code -> excluded
        var p4 = Guid.NewGuid(); // canceled -> excluded
        var p5 = Guid.NewGuid(); // starts after onDate -> excluded
        var p6 = Guid.NewGuid(); // other mission -> excluded

        var docOpen = Guid.NewGuid();
        var docClose = Guid.NewGuid();
        var reasonCodeId = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();

            var ep1 = SeedEpisode(db, p1, openedAt, null, now);
            var ep2 = SeedEpisode(db, p2, openedAt, null, now);
            var ep3 = SeedEpisode(db, p3, openedAt, null, now);
            var ep4 = SeedEpisode(db, p4, openedAt, null, now);
            var ep5 = SeedEpisode(db, p5, openedAt, null, now);
            var ep6 = SeedEpisode(db, p6, openedAt, null, now);

            db.TimesheetTaskSpans.Add(NewSpan(ep1.Id, p1, missionId, "A Person",
                from: new DateOnly(2026, 02, 18), toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: docOpen, closedByDocId: null, closedByCodeId: null, nowUtc: now));

            db.TimesheetTaskSpans.Add(NewSpan(ep2.Id, p2, missionId, "B ClosedByDoc",
                from: new DateOnly(2026, 02, 18), toExclusive: new DateOnly(2026, 02, 21),
                status: DocumentStatus.Active,
                openedByDocId: docOpen, closedByDocId: docClose, closedByCodeId: null, nowUtc: now));

            db.TimesheetTaskSpans.Add(NewSpan(ep3.Id, p3, missionId, "C ClosedByCode",
                from: new DateOnly(2026, 02, 18), toExclusive: new DateOnly(2026, 02, 21),
                status: DocumentStatus.Active,
                openedByDocId: docOpen, closedByDocId: null, closedByCodeId: reasonCodeId, nowUtc: now));

            db.TimesheetTaskSpans.Add(NewSpan(ep4.Id, p4, missionId, "D Canceled",
                from: new DateOnly(2026, 02, 18), toExclusive: null,
                status: DocumentStatus.Canceled,
                openedByDocId: docOpen, closedByDocId: null, closedByCodeId: null, nowUtc: now));

            db.TimesheetTaskSpans.Add(NewSpan(ep5.Id, p5, missionId, "E FutureStart",
                from: new DateOnly(2026, 02, 22), toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: docOpen, closedByDocId: null, closedByCodeId: null, nowUtc: now));

            db.TimesheetTaskSpans.Add(NewSpan(ep6.Id, p6, otherMissionId, "F OtherMission",
                from: new DateOnly(2026, 02, 18), toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: docOpen, closedByDocId: null, closedByCodeId: null, nowUtc: now));

            await db.SaveChangesAsync();
        }

        var closable = await repo.GetActiveMissionClosablePersonsAsync(missionId, onDate);

        Assert.Single(closable);
        Assert.Equal(p1, closable[0].PersonId);
        Assert.Equal("A Person", closable[0].FullName);
        Assert.Equal(new DateOnly(2026, 02, 18), closable[0].FromDate);
    }

    //======================================================================
    // GetFreePersonForMissionsAsync
    //======================================================================

    [Fact]
    public async Task GetFreePersonForMissionsAsync_ReturnsReadyWithoutActiveTask()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var onDate = new DateOnly(2026, 02, 10);

        var p1 = NewPerson("801", "Alpha Free");
        var p2 = NewPerson("802", "Bravo Busy");
        var p3 = NewPerson("803", "Charlie NotReady");
        var p4 = NewPerson("804", "Delta DeletedEntry");
        var p5 = NewPerson("805", "Echo Overridden");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();

            // codes required by repo (ready must exist)
            var readyCodeId = EnsureCode(db, TimesheetSystemCodes.ReadyToCombatTask, now);
            var baseCodeId = EnsureCode(db, TimesheetSystemCodes.BaseState, now);

            // persons
            db.PersonRead.AddRange(p1, p2, p3, p4, p5);

            // episodes
            var ts1 = SeedEpisode(db, p1.Id, new DateOnly(2026, 02, 01), null, now).Id;
            var ts2 = SeedEpisode(db, p2.Id, new DateOnly(2026, 02, 01), null, now).Id;
            var ts3 = SeedEpisode(db, p3.Id, new DateOnly(2026, 02, 01), null, now).Id;
            var ts4 = SeedEpisode(db, p4.Id, new DateOnly(2026, 02, 01), null, now).Id;
            var ts5 = SeedEpisode(db, p5.Id, new DateOnly(2026, 02, 01), null, now).Id;

            // P1: ready + no task => included
            db.TimesheetEntries.Add(NewEntry(ts1, p1.Id, readyCodeId, from: new DateOnly(2026, 02, 01), to: null, nowUtc: now, isDeleted: false));

            // P2: ready + HAS active task => excluded
            db.TimesheetEntries.Add(NewEntry(ts2, p2.Id, readyCodeId, new DateOnly(2026, 02, 01), null, now, false));
            db.TimesheetTaskSpans.Add(NewSpan(ts2, p2.Id, Guid.NewGuid(), p2.FullName,
                from: new DateOnly(2026, 02, 09), toExclusive: null,
                status: DocumentStatus.Active,
                openedByDocId: Guid.NewGuid(), closedByDocId: null, closedByCodeId: null, nowUtc: now));

            // P3: NOT ready => excluded
            db.TimesheetEntries.Add(NewEntry(ts3, p3.Id, baseCodeId, new DateOnly(2026, 02, 01), null, now, false));

            // P4: ready entry but deleted => excluded
            db.TimesheetEntries.Add(NewEntry(ts4, p4.Id, readyCodeId, new DateOnly(2026, 02, 01), null, now, isDeleted: true));

            // P5: ready then overridden by base later => excluded
            db.TimesheetEntries.Add(NewEntry(ts5, p5.Id, readyCodeId, new DateOnly(2026, 02, 01), null, now, false));
            db.TimesheetEntries.Add(NewEntry(ts5, p5.Id, baseCodeId, new DateOnly(2026, 02, 05), null, now.AddMinutes(1), false));

            await db.SaveChangesAsync();
        }

        var result = await repo.GetFreePersonForMissionsAsync(onDate);

        Assert.Single(result);
        Assert.Equal(p1.Id, result[0].PersonId);
        Assert.Equal(p1.Rnokpp, result[0].Rnokpp);
        Assert.Equal(p1.FullName, result[0].FullName);
    }

    [Fact]
    public async Task GetFreePersonForMissionsAsync_Throws_WhenReadyCodeMissing()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.GetFreePersonForMissionsAsync(new DateOnly(2026, 02, 10)));
    }

    [Fact]
    public async Task Methods_Throw_WhenOnDateDefault()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveMissionPersonsAsync(Guid.NewGuid(), default));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveMissionPersonsByDocumentAsync(Guid.NewGuid(), default));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetFreePersonForMissionsAsync(default));
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static DateTime Utc(int y, int m, int d, int hh, int mm)
        => new(y, m, d, hh, mm, 0, DateTimeKind.Utc);

    private static TimeSheetAggregate SeedEpisode(
        AppDbContext db,
        Guid personId,
        DateOnly openedAt,
        DateOnly? closedAt,
        DateTime nowUtc)
    {
        var ep = new TimeSheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        db.TimeSheets.Add(ep);
        return ep;
    }

    private static TimesheetTaskSpan NewSpan(
        Guid timesheetId,
        Guid personId,
        Guid missionId,
        string fullName,
        DateOnly from,
        DateOnly? toExclusive,
        DocumentStatus status,
        Guid openedByDocId,
        Guid? closedByDocId,
        Guid? closedByCodeId,
        DateTime nowUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            PersonId = personId,
            MissionId = missionId,

            OpenedByCombatTaskDocumentId = openedByDocId,
            ClosedByCombatTaskDocumentId = closedByDocId,

            FromDate = from,
            ToDate = toExclusive,
            Status = status,

            ClosedByCodeId = closedByCodeId,
            ClosedReference = null,

            Rnokpp = "0000000000",
            FullName = fullName,
            Rank = null,
            Position = null,
            Weapon = null,
            Callsign = null,

            CreatedBy = "seed",
            CreatedAtUtc = nowUtc,
            UpdatedBy = "seed",
            UpdatedAtUtc = nowUtc
        };

    private static TimesheetEntry NewEntry(
        Guid timesheetId,
        Guid personId,
        Guid codeId,
        DateOnly from,
        DateOnly? to,
        DateTime nowUtc,
        bool isDeleted)
        => new()
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            PersonId = personId,
            TimesheetCodeDefinitionId = codeId,
            From = from,
            To = to,

            CreatedBy = "seed",
            CreatedAtUtc = nowUtc,

            IsDeleted = isDeleted,
            DeletedBy = isDeleted ? "seed" : null,
            DeletedAtUtc = isDeleted ? nowUtc : null,
            DeleteReason = isDeleted ? "test" : null
        };

    private static Guid EnsureCode(AppDbContext db, string code, DateTime nowUtc)
    {
        var existing = db.TimesheetCodes.SingleOrDefault(x => x.Code == code);
        if (existing is not null)
            return existing.Id;

        var e = new TimesheetCodeDefinition
        {
            Id = Guid.NewGuid(),
            Code = code,
            Title = code,
            Description = null,
            SortOrder = 0,
            Priority = 0,
            IsTerminal = false,
            IsActive = true,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        db.TimesheetCodes.Add(e);
        return e.Id;
    }

    private static PersonReadModel NewPerson(string rnokpp, string fullName)
        => new()
        {
            Id = Guid.NewGuid(),
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = EnrollmentKind.Unit,
            EnrollmentReference = null,

            Rnokpp = rnokpp,
            LastName = fullName,
            FirstName = fullName,
            MiddleName = null,
            FullName = fullName,

            Rank = "R",
            PositionSort = 0,
            Position = "P",
            Bzvp = null,
            Weapon = "W",
            Callsign = "C",

            EnrolledAt = new DateOnly(2026, 02, 01),
            ExcludedAt = null,

            Version = 1,
            UpdatedAtUtc = Utc(2026, 02, 17, 10, 00)
        };
}
