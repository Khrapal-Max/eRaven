//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignmentRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class MissionAssignmentRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 01, 12, 0, 0, DateTimeKind.Utc);

    //======================================================================
    // Helpers
    //======================================================================

    private static MissionAssignment NewOpenAssignment(
        Guid personId,
        Guid missionId,
        DateOnly from,
        Guid sourceStartDocumentId,
        Guid sourceStartDetailsId)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            MissionId = missionId,
            From = from,
            To = null,
            SourceStartDocumentId = sourceStartDocumentId,
            SourceStartDetailsId = sourceStartDetailsId,
            SourceEndDocumentId = null,
            SourceEndDetailsId = null
        };

    private static CombatTaskPostedDetailsDto NewPosted(
        Guid documentId,
        Guid combatTaskId,
        Guid detailsId,
        CombatTaskDetailsKind kind,
        DateOnly effectiveAt,
        Guid personId,
        Guid missionId)
        => new(
            DocumentId: documentId,
            CombatTaskId: combatTaskId,
            DetailsId: detailsId,
            MissionId: missionId,
            PersonId: personId,
            Kind: kind,
            EffectiveAt: effectiveAt
        );

    private static PersonReadModel NewPersonRead(Guid id, string rnokpp, string fullName, string? callsign)
        => new()
        {
            Id = id,
            Rnokpp = rnokpp,
            FullName = fullName,
            Callsign = callsign,

            // Якщо в твоїй PersonReadModel цих полів нема / інші required — підправ тут.
            Lifecycle = PersonLifecycle.Enrolled,
            UpdatedAtUtc = NowUtc
        };

    //======================================================================
    // ApplyPostedLinesAsync
    //======================================================================

    [Fact]
    public async Task ApplyPostedLinesAsync_Start_creates_open_assignment()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var docId = Guid.NewGuid();
        var detailsId = Guid.NewGuid();
        var combatTaskId = Guid.NewGuid();

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyPostedLinesAsync([
            NewPosted(
                documentId: docId,
                combatTaskId: combatTaskId,
                detailsId: detailsId,
                kind: CombatTaskDetailsKind.Start,
                effectiveAt: new DateOnly(2026, 2, 2),
                personId: personId,
                missionId: missionId)
        ]);

        await using var db = await tdb.Factory.CreateDbContextAsync();
        var stored = await db.MissionAssignments.AsNoTracking().ToListAsync();

        Assert.Single(stored);

        var a = stored[0];
        Assert.Equal(personId, a.PersonId);
        Assert.Equal(missionId, a.MissionId);
        Assert.Equal(new DateOnly(2026, 2, 2), a.From);
        Assert.Null(a.To);

        Assert.Equal(docId, a.SourceStartDocumentId);
        Assert.Equal(detailsId, a.SourceStartDetailsId);
        Assert.Null(a.SourceEndDocumentId);
        Assert.Null(a.SourceEndDetailsId);
    }

    [Fact]
    public async Task ApplyPostedLinesAsync_End_closes_open_assignment_and_sets_source_end()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var startDocId = Guid.NewGuid();
        var startDetailsId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(NewOpenAssignment(
                personId: personId,
                missionId: missionId,
                from: new DateOnly(2026, 2, 2),
                sourceStartDocumentId: startDocId,
                sourceStartDetailsId: startDetailsId));

            await db.SaveChangesAsync();
        }

        var endDocId = Guid.NewGuid();
        var endDetailsId = Guid.NewGuid();
        var combatTaskId = Guid.NewGuid();

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyPostedLinesAsync([
            NewPosted(
                documentId: endDocId,
                combatTaskId: combatTaskId,
                detailsId: endDetailsId,
                kind: CombatTaskDetailsKind.End,
                effectiveAt: new DateOnly(2026, 2, 7),
                personId: personId,
                missionId: missionId)
        ]);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var stored = await db2.MissionAssignments.AsNoTracking()
            .Where(x => x.PersonId == personId && x.MissionId == missionId)
            .ToListAsync();

        Assert.Single(stored);

        var a = stored[0];
        Assert.Equal(new DateOnly(2026, 2, 2), a.From);
        Assert.Equal(new DateOnly(2026, 2, 7), a.To);

        Assert.Equal(startDocId, a.SourceStartDocumentId);
        Assert.Equal(startDetailsId, a.SourceStartDetailsId);
        Assert.Equal(endDocId, a.SourceEndDocumentId);
        Assert.Equal(endDetailsId, a.SourceEndDetailsId);
    }

    [Fact]
    public async Task ApplyPostedLinesAsync_orders_End_before_Start_on_same_date()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var missionA = Guid.NewGuid();
        var missionB = Guid.NewGuid();

        // Open mission A
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(NewOpenAssignment(
                personId: personId,
                missionId: missionA,
                from: new DateOnly(2026, 2, 2),
                sourceStartDocumentId: Guid.NewGuid(),
                sourceStartDetailsId: Guid.NewGuid()));

            await db.SaveChangesAsync();
        }

        var date = new DateOnly(2026, 2, 7);

        // Intentionally provide input in "wrong" order: Start then End.
        var startDoc = Guid.NewGuid();
        var startDetails = Guid.NewGuid();
        var endDoc = Guid.NewGuid();
        var endDetails = Guid.NewGuid();
        var combatTaskId = Guid.NewGuid();

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyPostedLinesAsync([
            NewPosted(startDoc, combatTaskId, startDetails, CombatTaskDetailsKind.Start, date, personId, missionB),
            NewPosted(endDoc,   combatTaskId, endDetails,   CombatTaskDetailsKind.End,   date, personId, missionA)
        ]);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var all = await db2.MissionAssignments.AsNoTracking()
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.From)
            .ThenBy(x => x.MissionId)
            .ToListAsync();

        Assert.Equal(2, all.Count);

        var a = all.Single(x => x.MissionId == missionA);
        Assert.Equal(new DateOnly(2026, 2, 2), a.From);
        Assert.Equal(date, a.To);
        Assert.Equal(endDoc, a.SourceEndDocumentId);
        Assert.Equal(endDetails, a.SourceEndDetailsId);

        var b = all.Single(x => x.MissionId == missionB);
        Assert.Equal(date, b.From);
        Assert.Null(b.To);
        Assert.Equal(startDoc, b.SourceStartDocumentId);
        Assert.Equal(startDetails, b.SourceStartDetailsId);
    }

    [Fact]
    public async Task ApplyPostedLinesAsync_throws_when_Start_and_person_has_open_mission()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var openMission = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(NewOpenAssignment(
                personId: personId,
                missionId: openMission,
                from: new DateOnly(2026, 2, 2),
                sourceStartDocumentId: Guid.NewGuid(),
                sourceStartDetailsId: Guid.NewGuid()));

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.ApplyPostedLinesAsync([
            NewPosted(
                documentId: Guid.NewGuid(),
                combatTaskId: Guid.NewGuid(),
                detailsId: Guid.NewGuid(),
                kind: CombatTaskDetailsKind.Start,
                effectiveAt: new DateOnly(2026, 2, 7),
                personId: personId,
                missionId: Guid.NewGuid())
        ]));

        Assert.Contains("вже має активну місію", ex.Message);
    }

    [Fact]
    public async Task ApplyPostedLinesAsync_throws_when_End_without_open_assignment()
    {
        await using var tdb = new SqliteTestDb();

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.ApplyPostedLinesAsync([
            NewPosted(
                documentId: Guid.NewGuid(),
                combatTaskId: Guid.NewGuid(),
                detailsId: Guid.NewGuid(),
                kind: CombatTaskDetailsKind.End,
                effectiveAt: new DateOnly(2026, 2, 7),
                personId: Guid.NewGuid(),
                missionId: Guid.NewGuid())
        ]));

        Assert.Contains("немає відкритого призначення", ex.Message);
    }

    [Fact]
    public async Task ApplyPostedLinesAsync_throws_when_End_before_start()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(NewOpenAssignment(
                personId: personId,
                missionId: missionId,
                from: new DateOnly(2026, 2, 10),
                sourceStartDocumentId: Guid.NewGuid(),
                sourceStartDetailsId: Guid.NewGuid()));

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.ApplyPostedLinesAsync([
            NewPosted(
                documentId: Guid.NewGuid(),
                combatTaskId: Guid.NewGuid(),
                detailsId: Guid.NewGuid(),
                kind: CombatTaskDetailsKind.End,
                effectiveAt: new DateOnly(2026, 2, 7),
                personId: personId,
                missionId: missionId)
        ]));

        Assert.Contains("раніше старту", ex.Message);
    }

    //======================================================================
    // Reads: GetActiveByMissionAsync (NEW)
    //======================================================================

    [Fact]
    public async Task GetActiveByMissionAsync_throws_when_missionId_empty()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveByMissionAsync(Guid.Empty, new DateOnly(2026, 2, 7)));
    }

    [Fact]
    public async Task GetActiveByMissionAsync_throws_when_onDate_default()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveByMissionAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task GetActiveByMissionAsync_returns_only_open_ended_started_on_or_before_date_sorted_and_mapped()
    {
        await using var tdb = new SqliteTestDb();

        var missionId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 2, 7);

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var pClosed = Guid.NewGuid();
        var pFuture = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.PersonRead.AddRange(
                NewPersonRead(p1, "1111111111", "Alpha", "A"),
                NewPersonRead(p2, "2222222222", "Bravo", null),
                NewPersonRead(pClosed, "3333333333", "Closed", "C"),
                NewPersonRead(pFuture, "4444444444", "Future", "F"));

            db.MissionAssignments.AddRange(
                // active
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = p2,
                    MissionId = missionId,
                    From = new DateOnly(2026, 2, 2),
                    To = null,
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                },
                // active earlier
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = p1,
                    MissionId = missionId,
                    From = new DateOnly(2026, 2, 1),
                    To = null,
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                },
                // closed -> must NOT return (бо To != null)
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = pClosed,
                    MissionId = missionId,
                    From = new DateOnly(2026, 2, 1),
                    To = new DateOnly(2026, 2, 3),
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                },
                // open-ended but starts in future -> must NOT return (From > onDate)
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = pFuture,
                    MissionId = missionId,
                    From = new DateOnly(2026, 2, 10),
                    To = null,
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var res = await repo.GetActiveByMissionAsync(missionId, onDate);

        // only p1, p2
        Assert.Equal(2, res.Count);

        // sorted by From then PersonId
        Assert.Equal(p1, res[0].PersonId);
        Assert.Equal(new DateOnly(2026, 2, 1), res[0].From);
        Assert.Equal("1111111111", res[0].Rnokpp);
        Assert.Equal("Alpha", res[0].FullName);

        Assert.Equal(p2, res[1].PersonId);
        Assert.Equal(new DateOnly(2026, 2, 2), res[1].From);
        Assert.Equal("2222222222", res[1].Rnokpp);
        Assert.Equal("Bravo", res[1].FullName);
    }

    //======================================================================
    // Reads: existing methods
    //======================================================================

    [Fact]
    public async Task GetActiveForPersonAsync_returns_latest_active_on_date()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();

        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.AddRange(
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    MissionId = m1,
                    From = new DateOnly(2026, 2, 1),
                    To = new DateOnly(2026, 2, 3),
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                },
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    MissionId = m2,
                    From = new DateOnly(2026, 2, 4),
                    To = null,
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var active = await repo.GetActiveForPersonAsync(personId, new DateOnly(2026, 2, 10));

        Assert.NotNull(active);
        Assert.Equal(m2, active!.MissionId);
        Assert.Null(active.To);
        Assert.Equal(new DateOnly(2026, 2, 4), active.From);
    }

    [Fact]
    public async Task GetPersonAssignmentsAsync_returns_overlaps_in_period_sorted()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.AddRange(
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    MissionId = m2,
                    From = new DateOnly(2026, 2, 10),
                    To = null,
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                },
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = personId,
                    MissionId = m1,
                    From = new DateOnly(2026, 2, 1),
                    To = new DateOnly(2026, 2, 5),
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var res = await repo.GetPersonAssignmentsAsync(
            personId,
            from: new DateOnly(2026, 2, 1),
            to: new DateOnly(2026, 2, 28));

        Assert.Equal(2, res.Count);
        Assert.Equal(new DateOnly(2026, 2, 1), res[0].From);
        Assert.Equal(new DateOnly(2026, 2, 10), res[1].From);
    }

    //======================================================================
    // GetFreePersonForMissionsAsync (coverage)
    //======================================================================

    [Fact]
    public async Task GetFreePersonForMissionsAsync_throws_when_onDate_default()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetFreePersonForMissionsAsync(default));
    }

    [Fact]
    public async Task GetFreePersonForMissionsAsync_returns_all_when_no_open_assignments()
    {
        await using var tdb = new SqliteTestDb();

        var onDate = new DateOnly(2026, 2, 7);
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.PersonRead.AddRange(
                NewPersonRead(p1, "1111111111", "Alpha", "A"),
                NewPersonRead(p2, "2222222222", "Bravo", null));

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);
        var free = await repo.GetFreePersonForMissionsAsync(onDate);

        Assert.Equal(2, free.Count);
        Assert.Contains(free, x => x.PersonId == p1);
        Assert.Contains(free, x => x.PersonId == p2);
    }

    [Fact]
    public async Task GetFreePersonForMissionsAsync_excludes_only_persons_with_open_assignments_started_on_or_before_date()
    {
        await using var tdb = new SqliteTestDb();

        var onDate = new DateOnly(2026, 2, 7);

        var pFree = Guid.NewGuid();
        var pOpen = Guid.NewGuid();
        var pFutureOpen = Guid.NewGuid(); // open-ended але From > onDate => FREE за поточною логікою

        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.PersonRead.AddRange(
                NewPersonRead(pFree, "1111111111", "Free", "F"),
                NewPersonRead(pOpen, "2222222222", "Open", "O"),
                NewPersonRead(pFutureOpen, "3333333333", "FutureOpen", "X"));

            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = pOpen,
                MissionId = m1,
                From = new DateOnly(2026, 2, 1),
                To = null,
                SourceStartDocumentId = Guid.NewGuid(),
                SourceStartDetailsId = Guid.NewGuid()
            });

            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = pFutureOpen,
                MissionId = m2,
                From = new DateOnly(2026, 2, 10), // after onDate
                To = null,
                SourceStartDocumentId = Guid.NewGuid(),
                SourceStartDetailsId = Guid.NewGuid()
            });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);
        var free = await repo.GetFreePersonForMissionsAsync(onDate);

        Assert.Equal(2, free.Count);
        Assert.Contains(free, x => x.PersonId == pFree);
        Assert.Contains(free, x => x.PersonId == pFutureOpen);
        Assert.DoesNotContain(free, x => x.PersonId == pOpen);
    }
}
