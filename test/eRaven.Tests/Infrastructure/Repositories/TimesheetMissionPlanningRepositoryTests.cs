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
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="TimesheetMissionPlanningRepository"/>.
///
/// <para>
/// Фіксуємо read-стратегію планування/звітів по місіях на основі фактів табеля:
/// <list type="bullet">
/// <item><description>ActiveMissionPersons: бере лише active (Status != Canceled) spans з half-open інтервалом [FromDate..ToDate).</description></item>
/// <item><description>ActiveMissionPersonsByDocument: фільтр по openedBy/closedBy документу + half-open інтервал.</description></item>
/// <item><description>FreePersonForMissions: люди з поточним кодом "30" (ReadyToCombatTask) та без active span на дату.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetMissionPlanningRepositoryTests
{
    //======================================================================
    // GetActiveMissionPersonsAsync
    //======================================================================

    /// <summary>
    /// Повертає лише активні (не canceled) spans для місії на дату onDate.
    /// Half-open: якщо onDate == ToDate, span НЕ активний.
    /// </summary>
    [Fact]
    public async Task GetActiveMissionPersonsAsync_ReturnsOnlyActiveNonCanceled_HalfOpen()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var onDate = new DateOnly(2026, 02, 10);

        var missionId = Guid.NewGuid();

        // person A -> active (open-ended)
        var pA = NewPerson("111", "Alpha A");
        var tsA = await SeedEpisodeAsync(testDb, pA.Id, openedAt: new DateOnly(2026, 02, 01), closedAt: null, nowUtc: now);
        await SeedPersonAsync(testDb, pA);

        await SeedSpanAsync(testDb, new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsA,
            PersonId = pA.Id,
            MissionId = missionId,
            OpenedByCombatTaskDocumentId = Guid.NewGuid(),
            ClosedByCombatTaskDocumentId = null,
            FromDate = new DateOnly(2026, 02, 01),
            ToDate = null,
            Status = DocumentStatus.Active,
            Rnokpp = pA.Rnokpp,
            FullName = pA.FullName,
            Rank = pA.Rank,
            Position = pA.Position,
            Weapon = pA.Weapon,
            Callsign = pA.Callsign,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now
        });

        // person B -> ToDate == onDate (exclusive) => NOT active on onDate
        var pB = NewPerson("222", "Bravo B");
        var tsB = await SeedEpisodeAsync(testDb, pB.Id, new DateOnly(2026, 02, 01), null, now);
        await SeedPersonAsync(testDb, pB);

        await SeedSpanAsync(testDb, new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsB,
            PersonId = pB.Id,
            MissionId = missionId,
            OpenedByCombatTaskDocumentId = Guid.NewGuid(),
            ClosedByCombatTaskDocumentId = null,
            FromDate = new DateOnly(2026, 02, 05),
            ToDate = onDate, // half-open => onDate is excluded
            Status = DocumentStatus.Active,
            Rnokpp = pB.Rnokpp,
            FullName = pB.FullName,
            Rank = pB.Rank,
            Position = pB.Position,
            Weapon = pB.Weapon,
            Callsign = pB.Callsign,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now
        });

        // person C -> canceled => excluded
        var pC = NewPerson("333", "Charlie C");
        var tsC = await SeedEpisodeAsync(testDb, pC.Id, new DateOnly(2026, 02, 01), null, now);
        await SeedPersonAsync(testDb, pC);

        await SeedSpanAsync(testDb, new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsC,
            PersonId = pC.Id,
            MissionId = missionId,
            OpenedByCombatTaskDocumentId = Guid.NewGuid(),
            ClosedByCombatTaskDocumentId = null,
            FromDate = new DateOnly(2026, 02, 09),
            ToDate = new DateOnly(2026, 02, 12),
            Status = DocumentStatus.Canceled,
            Rnokpp = pC.Rnokpp,
            FullName = pC.FullName,
            Rank = pC.Rank,
            Position = pC.Position,
            Weapon = pC.Weapon,
            Callsign = pC.Callsign,
            ClosedByCodeId = Guid.NewGuid(),
            ClosedReference = "X",
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now
        });

        // person D -> different mission => excluded
        var pD = NewPerson("444", "Delta D");
        var tsD = await SeedEpisodeAsync(testDb, pD.Id, new DateOnly(2026, 02, 01), null, now);
        await SeedPersonAsync(testDb, pD);

        await SeedSpanAsync(testDb, new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsD,
            PersonId = pD.Id,
            MissionId = Guid.NewGuid(), // other mission
            OpenedByCombatTaskDocumentId = Guid.NewGuid(),
            ClosedByCombatTaskDocumentId = null,
            FromDate = new DateOnly(2026, 02, 01),
            ToDate = null,
            Status = DocumentStatus.Active,
            Rnokpp = pD.Rnokpp,
            FullName = pD.FullName,
            Rank = pD.Rank,
            Position = pD.Position,
            Weapon = pD.Weapon,
            Callsign = pD.Callsign,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now
        });

        var result = await repo.GetActiveMissionPersonsAsync(missionId, onDate);

        Assert.Single(result);

        var dto = result[0];
        Assert.Equal(pA.Id, dto.PersonId);
        Assert.Equal(pA.Rnokpp, dto.Rnokpp);
        Assert.Equal(pA.FullName, dto.FullName);
        Assert.Equal(new DateOnly(2026, 02, 01), dto.From);
    }

    /// <summary>
    /// Результат сортується за FullName, потім за From.
    /// </summary>
    [Fact]
    public async Task GetActiveMissionPersonsAsync_OrdersByFullName_ThenByFrom()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var onDate = new DateOnly(2026, 02, 10);

        var missionId = Guid.NewGuid();

        // Two active persons (names ensure ordering)
        var p1 = NewPerson("555", "Alpha");
        var p2 = NewPerson("666", "Alpha"); // same name, different From => order by From

        await SeedPersonAsync(testDb, p1);
        await SeedPersonAsync(testDb, p2);

        var ts1 = await SeedEpisodeAsync(testDb, p1.Id, new DateOnly(2026, 02, 01), null, now);
        var ts2 = await SeedEpisodeAsync(testDb, p2.Id, new DateOnly(2026, 02, 01), null, now);

        await SeedSpanAsync(testDb, NewSpan(ts1, p1, missionId, from: new DateOnly(2026, 02, 05), toExclusive: null, now));
        await SeedSpanAsync(testDb, NewSpan(ts2, p2, missionId, from: new DateOnly(2026, 02, 03), toExclusive: null, now));

        var result = await repo.GetActiveMissionPersonsAsync(missionId, onDate);

        Assert.Equal(2, result.Count);
        Assert.Equal(p2.Id, result[0].PersonId); // Alpha, From 02-03
        Assert.Equal(p1.Id, result[1].PersonId); // Alpha, From 02-05
    }

    //======================================================================
    // GetActiveMissionPersonsByDocumentAsync
    //======================================================================

    /// <summary>
    /// Фільтр по документу працює і для openedBy, і для closedBy. Half-open інтервал також враховується.
    /// </summary>
    [Fact]
    public async Task GetActiveMissionPersonsByDocumentAsync_ReturnsOpenedOrClosedByDocument()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var onDate = new DateOnly(2026, 02, 10);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        // A: openedBy == documentId (active)
        var pA = NewPerson("701", "Alpha Doc");
        await SeedPersonAsync(testDb, pA);
        var tsA = await SeedEpisodeAsync(testDb, pA.Id, new DateOnly(2026, 02, 01), null, now);

        await SeedSpanAsync(testDb, new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsA,
            PersonId = pA.Id,
            MissionId = missionId,
            OpenedByCombatTaskDocumentId = documentId,
            ClosedByCombatTaskDocumentId = null,
            FromDate = new DateOnly(2026, 02, 05),
            ToDate = null,
            Status = DocumentStatus.Active,
            Rnokpp = pA.Rnokpp,
            FullName = pA.FullName,
            Rank = pA.Rank,
            Position = pA.Position,
            Weapon = pA.Weapon,
            Callsign = pA.Callsign,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now
        });

        // B: closedBy == documentId (active on onDate; ToDate > onDate)
        var pB = NewPerson("702", "Bravo Doc");
        await SeedPersonAsync(testDb, pB);
        var tsB = await SeedEpisodeAsync(testDb, pB.Id, new DateOnly(2026, 02, 01), null, now);

        await SeedSpanAsync(testDb, new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsB,
            PersonId = pB.Id,
            MissionId = missionId,
            OpenedByCombatTaskDocumentId = Guid.NewGuid(),
            ClosedByCombatTaskDocumentId = documentId,
            FromDate = new DateOnly(2026, 02, 01),
            ToDate = new DateOnly(2026, 02, 11),
            Status = DocumentStatus.Active,
            Rnokpp = pB.Rnokpp,
            FullName = pB.FullName,
            Rank = pB.Rank,
            Position = pB.Position,
            Weapon = pB.Weapon,
            Callsign = pB.Callsign,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now
        });

        // C: unrelated document => excluded
        var pC = NewPerson("703", "Charlie Other");
        await SeedPersonAsync(testDb, pC);
        var tsC = await SeedEpisodeAsync(testDb, pC.Id, new DateOnly(2026, 02, 01), null, now);

        await SeedSpanAsync(testDb, NewSpan(tsC, pC, missionId, from: new DateOnly(2026, 02, 01), toExclusive: null, now));

        // D: half-open boundary: ToDate == onDate => excluded even if doc matches
        var pD = NewPerson("704", "Delta Boundary");
        await SeedPersonAsync(testDb, pD);
        var tsD = await SeedEpisodeAsync(testDb, pD.Id, new DateOnly(2026, 02, 01), null, now);

        await SeedSpanAsync(testDb, new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = tsD,
            PersonId = pD.Id,
            MissionId = missionId,
            OpenedByCombatTaskDocumentId = documentId,
            ClosedByCombatTaskDocumentId = null,
            FromDate = new DateOnly(2026, 02, 01),
            ToDate = onDate, // excluded by half-open
            Status = DocumentStatus.Active,
            Rnokpp = pD.Rnokpp,
            FullName = pD.FullName,
            Rank = pD.Rank,
            Position = pD.Position,
            Weapon = pD.Weapon,
            Callsign = pD.Callsign,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now
        });

        var result = await repo.GetActiveMissionPersonsByDocumentAsync(documentId, onDate);

        Assert.Equal(2, result.Count);

        // ordered by FullName
        Assert.Equal(pA.Id, result[0].PersonId);
        Assert.Equal(pB.Id, result[1].PersonId);
    }

    //======================================================================
    // GetActiveMissionClosablePersonsAsync
    //======================================================================
    /// <summary>
    /// Повертає тіх у кого не закриті місії 
    /// </summary>

    [Fact]
    public async Task GetActiveMissionClosablePersonsAsync_ReturnsOnlyOpenActiveNotFinalizedSpans()
    {
        await using var testDb = new SqliteTestDb();

        var missionId = Guid.NewGuid();
        var otherMissionId = Guid.NewGuid();

        var onDate = new DateOnly(2026, 02, 20);
        var openedAt = new DateOnly(2026, 02, 01);

        var now = new DateTime(2026, 02, 18, 10, 00, 00, DateTimeKind.Utc);

        // Persons
        var p1 = Guid.NewGuid(); // ✅ open + active + not finalized -> must be returned
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

            // Episodes (FK target)
            var ep1 = await SeedEpisodeAsync(db, p1, openedAt, now);
            var ep2 = await SeedEpisodeAsync(db, p2, openedAt, now);
            var ep3 = await SeedEpisodeAsync(db, p3, openedAt, now);
            var ep4 = await SeedEpisodeAsync(db, p4, openedAt, now);
            var ep5 = await SeedEpisodeAsync(db, p5, openedAt, now);
            var ep6 = await SeedEpisodeAsync(db, p6, openedAt, now);

            // p1: open, active, not finalized => should be returned
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: ep1.Id,
                personId: p1,
                missionId: missionId,
                openedByDocId: docOpen,
                from: new DateOnly(2026, 02, 18),
                toExclusive: null,
                status: DocumentStatus.Active,
                closedByDocId: null,
                closedByCodeId: null,
                nowUtc: now,
                fullName: "A Person"));

            // p2: active on onDate but finalized by another document => excluded
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: ep2.Id,
                personId: p2,
                missionId: missionId,
                openedByDocId: docOpen,
                from: new DateOnly(2026, 02, 18),
                toExclusive: new DateOnly(2026, 02, 21), // half-open => includes 20.02
                status: DocumentStatus.Active,
                closedByDocId: docClose,
                closedByCodeId: null,
                nowUtc: now,
                fullName: "B ClosedByDoc"));

            // p3: active on onDate but finalized by reason code => excluded
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: ep3.Id,
                personId: p3,
                missionId: missionId,
                openedByDocId: docOpen,
                from: new DateOnly(2026, 02, 18),
                toExclusive: new DateOnly(2026, 02, 21),
                status: DocumentStatus.Active,
                closedByDocId: null,
                closedByCodeId: reasonCodeId,
                nowUtc: now,
                fullName: "C ClosedByCode"));

            // p4: canceled => excluded
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: ep4.Id,
                personId: p4,
                missionId: missionId,
                openedByDocId: docOpen,
                from: new DateOnly(2026, 02, 18),
                toExclusive: null,
                status: DocumentStatus.Canceled,
                closedByDocId: null,
                closedByCodeId: null,
                nowUtc: now,
                fullName: "D Canceled"));

            // p5: starts AFTER onDate => must be excluded (важлива перевірка!)
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: ep5.Id,
                personId: p5,
                missionId: missionId,
                openedByDocId: docOpen,
                from: new DateOnly(2026, 02, 22),
                toExclusive: null,
                status: DocumentStatus.Active,
                closedByDocId: null,
                closedByCodeId: null,
                nowUtc: now,
                fullName: "E FutureStart"));

            // p6: other mission => excluded
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: ep6.Id,
                personId: p6,
                missionId: otherMissionId,
                openedByDocId: docOpen,
                from: new DateOnly(2026, 02, 18),
                toExclusive: null,
                status: DocumentStatus.Active,
                closedByDocId: null,
                closedByCodeId: null,
                nowUtc: now,
                fullName: "F OtherMission"));

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        // Act
        var closable = await repo.GetActiveMissionClosablePersonsAsync(missionId, onDate);

        // Assert
        Assert.Single(closable);

        var x = closable[0];
        Assert.Equal(p1, x.PersonId);
        Assert.Equal("A Person", x.FullName);
        Assert.Equal(new DateOnly(2026, 02, 18), x.From);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static async Task<TimeSheetAggregate> SeedEpisodeAsync(
        AppDbContext db,
        Guid personId,
        DateOnly openedAt,
        DateTime nowUtc)
    {
        var ep = new TimeSheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = null,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc
        };

        db.TimeSheets.Add(ep);
        await db.SaveChangesAsync();
        return ep;
    }

    private static TimesheetTaskSpan NewSpan(
        Guid timesheetId,
        Guid personId,
        Guid missionId,
        Guid openedByDocId,
        DateOnly from,
        DateOnly? toExclusive,
        DocumentStatus status,
        Guid? closedByDocId,
        Guid? closedByCodeId,
        DateTime nowUtc,
        string fullName)
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

    //======================================================================
    // GetFreePersonForMissionsAsync
    //======================================================================

    /// <summary>
    /// Free persons: current code == ReadyToCombatTask ("30") AND no active task span on date.
    /// </summary>
    [Fact]
    public async Task GetFreePersonForMissionsAsync_ReturnsReadyWithoutActiveTask()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var now = Utc(2026, 02, 17, 10, 00);
        var onDate = new DateOnly(2026, 02, 10);

        // codes
        var readyCodeId = await SeedCodeAsync(testDb, TimesheetSystemCodes.ReadyToCombatTask);
        var baseCodeId = await SeedCodeAsync(testDb, TimesheetSystemCodes.BaseState);

        // P1: ready + no task => included
        var p1 = NewPerson("801", "Alpha Free");
        await SeedPersonAsync(testDb, p1);
        var ts1 = await SeedEpisodeAsync(testDb, p1.Id, new DateOnly(2026, 02, 01), null, now);
        await SeedEntryAsync(testDb, NewEntry(ts1, p1.Id, readyCodeId, from: new DateOnly(2026, 02, 01), to: null, now, isDeleted: false));

        // P2: ready + HAS active task => excluded
        var p2 = NewPerson("802", "Bravo Busy");
        await SeedPersonAsync(testDb, p2);
        var ts2 = await SeedEpisodeAsync(testDb, p2.Id, new DateOnly(2026, 02, 01), null, now);
        await SeedEntryAsync(testDb, NewEntry(ts2, p2.Id, readyCodeId, new DateOnly(2026, 02, 01), null, now, false));

        await SeedSpanAsync(testDb, new TimesheetTaskSpan
        {
            Id = Guid.NewGuid(),
            TimesheetId = ts2,
            PersonId = p2.Id,
            MissionId = Guid.NewGuid(),
            OpenedByCombatTaskDocumentId = Guid.NewGuid(),
            ClosedByCombatTaskDocumentId = null,
            FromDate = new DateOnly(2026, 02, 09),
            ToDate = null, // active on onDate
            Status = DocumentStatus.Active,
            Rnokpp = p2.Rnokpp,
            FullName = p2.FullName,
            Rank = p2.Rank,
            Position = p2.Position,
            Weapon = p2.Weapon,
            Callsign = p2.Callsign,
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now
        });

        // P3: NOT ready => excluded
        var p3 = NewPerson("803", "Charlie NotReady");
        await SeedPersonAsync(testDb, p3);
        var ts3 = await SeedEpisodeAsync(testDb, p3.Id, new DateOnly(2026, 02, 01), null, now);
        await SeedEntryAsync(testDb, NewEntry(ts3, p3.Id, baseCodeId, new DateOnly(2026, 02, 01), null, now, false));

        // P4: ready entry but deleted => excluded
        var p4 = NewPerson("804", "Delta DeletedEntry");
        await SeedPersonAsync(testDb, p4);
        var ts4 = await SeedEpisodeAsync(testDb, p4.Id, new DateOnly(2026, 02, 01), null, now);
        await SeedEntryAsync(testDb, NewEntry(ts4, p4.Id, readyCodeId, new DateOnly(2026, 02, 01), null, now, isDeleted: true));

        // P5: had ready, but later entry overrides to base => excluded (current code chosen by max From)
        var p5 = NewPerson("805", "Echo Overridden");
        await SeedPersonAsync(testDb, p5);
        var ts5 = await SeedEpisodeAsync(testDb, p5.Id, new DateOnly(2026, 02, 01), null, now);

        await SeedEntryAsync(testDb, NewEntry(ts5, p5.Id, readyCodeId, new DateOnly(2026, 02, 01), null, now, false));
        await SeedEntryAsync(testDb, NewEntry(ts5, p5.Id, baseCodeId, new DateOnly(2026, 02, 05), null, now.AddMinutes(1), false));

        var result = await repo.GetFreePersonForMissionsAsync(onDate);

        Assert.Single(result);

        var dto = result[0];
        Assert.Equal(p1.Id, dto.PersonId);
        Assert.Equal(p1.Rnokpp, dto.Rnokpp);
        Assert.Equal(p1.FullName, dto.FullName);
    }

    /// <summary>
    /// Якщо системний код ReadyToCombatTask ("30") відсутній у довіднику — метод падає (SingleAsync).
    /// Це важливий контракт: seed кодів має гарантувати наявність "30".
    /// </summary>
    [Fact]
    public async Task GetFreePersonForMissionsAsync_Throws_WhenReadyCodeMissing()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.GetFreePersonForMissionsAsync(new DateOnly(2026, 02, 10)));
    }

    /// <summary>
    /// Валідація аргументів: onDate != default.
    /// </summary>
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

    private static PersonReadModel NewPerson(string rnokpp, string fullName)
        => new()
        {
            Id = Guid.NewGuid(),
            Lifecycle = PersonLifecycle.Enrolled,
            Rnokpp = rnokpp,
            LastName = fullName,
            FirstName = fullName,
            FullName = fullName,
            Rank = "R",
            Position = "P",
            Weapon = "W",
            Callsign = "C",
            Version = 1,
            UpdatedAtUtc = Utc(2026, 02, 17, 10, 00)
        };

    private static TimesheetTaskSpan NewSpan(Guid timesheetId, PersonReadModel p, Guid missionId, DateOnly from, DateOnly? toExclusive, DateTime nowUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            PersonId = p.Id,
            MissionId = missionId,
            OpenedByCombatTaskDocumentId = Guid.NewGuid(),
            ClosedByCombatTaskDocumentId = null,
            FromDate = from,
            ToDate = toExclusive,
            Status = DocumentStatus.Active,
            Rnokpp = p.Rnokpp,
            FullName = p.FullName,
            Rank = p.Rank,
            Position = p.Position,
            Weapon = p.Weapon,
            Callsign = p.Callsign,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc,
            UpdatedBy = "seed",
            UpdatedAtUtc = nowUtc
        };

    private static TimesheetEntry NewEntry(Guid timesheetId, Guid personId, Guid codeId, DateOnly from, DateOnly? to, DateTime nowUtc, bool isDeleted)
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

    private static async Task<Guid> SeedCodeAsync(SqliteTestDb testDb, string code)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();

        var existing = await db.TimesheetCodes.SingleOrDefaultAsync(x => x.Code == code);
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
            CreatedAtUtc = Utc(2026, 02, 17, 10, 00)
        };

        db.TimesheetCodes.Add(e);
        await db.SaveChangesAsync();

        return e.Id;
    }

    private static async Task SeedPersonAsync(SqliteTestDb testDb, PersonReadModel p)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.PersonRead.Add(p);
        await db.SaveChangesAsync();
    }

    private static async Task<Guid> SeedEpisodeAsync(
        SqliteTestDb testDb,
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

        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimeSheets.Add(ep);
        await db.SaveChangesAsync();

        return ep.Id;
    }

    private static async Task SeedEntryAsync(SqliteTestDb testDb, TimesheetEntry entry)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimesheetEntries.Add(entry);
        await db.SaveChangesAsync();
    }

    private static async Task SeedSpanAsync(SqliteTestDb testDb, TimesheetTaskSpan span)
    {
        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimesheetTaskSpans.Add(span);
        await db.SaveChangesAsync();
    }
}
