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
        Guid sourceStartDetailsId,
        MissionAssignmentStatus status = MissionAssignmentStatus.Committed)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            MissionId = missionId,
            From = from,
            To = null,

            Status = status,

            SourceStartDocumentId = sourceStartDocumentId,
            SourceStartDetailsId = sourceStartDetailsId,
            SourceEndDocumentId = null,
            SourceEndDetailsId = null
        };

    private static ApplyCombatTaskDetailsDto NewApply(
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
            Lifecycle = PersonLifecycle.Enrolled,
            UpdatedAtUtc = NowUtc
        };

    //======================================================================
    // Write: ApplyDraftCombatTaskDocumentAsync
    //======================================================================

    [Fact]
    public async Task ApplyDraftCombatTaskDocumentAsync_Start_creates_planned_open_assignment()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var docId = Guid.NewGuid();
        var detailsId = Guid.NewGuid();
        var combatTaskId = Guid.NewGuid();

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyDraftCombatTaskDocumentAsync([
            NewApply(docId, combatTaskId, detailsId, CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), personId, missionId)
        ]);

        await using var db = await tdb.Factory.CreateDbContextAsync();
        var stored = await db.MissionAssignments.AsNoTracking().ToListAsync();

        Assert.Single(stored);

        var a = stored[0];
        Assert.Equal(personId, a.PersonId);
        Assert.Equal(missionId, a.MissionId);
        Assert.Equal(new DateOnly(2026, 2, 2), a.From);
        Assert.Null(a.To);

        Assert.Equal(MissionAssignmentStatus.Planned, a.Status);

        Assert.Equal(docId, a.SourceStartDocumentId);
        Assert.Equal(detailsId, a.SourceStartDetailsId);
        Assert.Null(a.SourceEndDocumentId);
        Assert.Null(a.SourceEndDetailsId);
    }

    [Fact]
    public async Task ApplyDraftCombatTaskDocumentAsync_End_closes_planned_assignment_and_sets_source_end()
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
                sourceStartDetailsId: startDetailsId,
                status: MissionAssignmentStatus.Planned));

            await db.SaveChangesAsync();
        }

        var endDocId = Guid.NewGuid();
        var endDetailsId = Guid.NewGuid();
        var combatTaskId = Guid.NewGuid();

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyDraftCombatTaskDocumentAsync([
            NewApply(endDocId, combatTaskId, endDetailsId, CombatTaskDetailsKind.End, new DateOnly(2026, 2, 7), personId, missionId)
        ]);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var stored = await db2.MissionAssignments.AsNoTracking()
            .Where(x => x.PersonId == personId && x.MissionId == missionId)
            .ToListAsync();

        Assert.Single(stored);

        var a = stored[0];
        Assert.Equal(new DateOnly(2026, 2, 2), a.From);
        Assert.Equal(new DateOnly(2026, 2, 7), a.To);

        // Draft закриває Planned і не робить Committed
        Assert.Equal(MissionAssignmentStatus.Planned, a.Status);

        Assert.Equal(startDocId, a.SourceStartDocumentId);
        Assert.Equal(startDetailsId, a.SourceStartDetailsId);
        Assert.Equal(endDocId, a.SourceEndDocumentId);
        Assert.Equal(endDetailsId, a.SourceEndDetailsId);
    }

    [Fact]
    public async Task ApplyDraftCombatTaskDocumentAsync_throws_when_End_targets_only_committed_open_assignment()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(NewOpenAssignment(
                personId: personId,
                missionId: missionId,
                from: new DateOnly(2026, 2, 2),
                sourceStartDocumentId: Guid.NewGuid(),
                sourceStartDetailsId: Guid.NewGuid(),
                status: MissionAssignmentStatus.Committed));

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.ApplyDraftCombatTaskDocumentAsync([
            NewApply(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.End, new DateOnly(2026, 2, 7), personId, missionId)
        ]));

        Assert.Contains("планов", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ApplyDraftCombatTaskDocumentAsync_throws_when_Start_and_person_has_open_committed_mission()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(NewOpenAssignment(
                personId: personId,
                missionId: Guid.NewGuid(),
                from: new DateOnly(2026, 2, 2),
                sourceStartDocumentId: Guid.NewGuid(),
                sourceStartDetailsId: Guid.NewGuid(),
                status: MissionAssignmentStatus.Committed));

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.ApplyDraftCombatTaskDocumentAsync([
            NewApply(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 7), personId, Guid.NewGuid())
        ]));

        Assert.Contains("вже має активну місію", ex.Message);
    }

    [Fact]
    public async Task ApplyDraftCombatTaskDocumentAsync_is_idempotent_for_existing_planned_start_by_detailsId()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var docId = Guid.NewGuid();
        var detailsId = Guid.NewGuid();
        var combatTaskId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                MissionId = missionId,
                From = new DateOnly(2026, 2, 1),
                To = null,
                Status = MissionAssignmentStatus.Planned,
                SourceStartDocumentId = docId,
                SourceStartDetailsId = detailsId
            });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        // re-apply with same DetailsId but updated date
        await repo.ApplyDraftCombatTaskDocumentAsync([
            NewApply(docId, combatTaskId, detailsId, CombatTaskDetailsKind.Start, new DateOnly(2026, 2, 2), personId, missionId)
        ]);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var stored = await db2.MissionAssignments.AsNoTracking()
            .Where(x => x.SourceStartDetailsId == detailsId)
            .ToListAsync();

        Assert.Single(stored);
        Assert.Equal(MissionAssignmentStatus.Planned, stored[0].Status);
        Assert.Equal(new DateOnly(2026, 2, 2), stored[0].From);
        Assert.Null(stored[0].To);
    }

    //======================================================================
    // Write: ApplyPostedCombatTaskDocumentAsync
    //======================================================================

    [Fact]
    public async Task ApplyPostedCombatTaskDocumentAsync_promotes_planned_to_committed_for_document()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        var startDetails1 = Guid.NewGuid();
        var startDetails2 = Guid.NewGuid();
        var endDetails2 = Guid.NewGuid();

        var otherDocId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            // Planned by SourceStartDocumentId == docId -> must be promoted
            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = p1,
                MissionId = m1,
                From = new DateOnly(2026, 2, 1),
                To = null,
                Status = MissionAssignmentStatus.Planned,
                SourceStartDocumentId = docId,
                SourceStartDetailsId = startDetails1
            });

            // Planned with End also in same doc -> must be promoted
            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = p2,
                MissionId = m2,
                From = new DateOnly(2026, 2, 2),
                To = new DateOnly(2026, 2, 7),
                Status = MissionAssignmentStatus.Planned,
                SourceStartDocumentId = docId,
                SourceStartDetailsId = startDetails2,
                SourceEndDocumentId = docId,
                SourceEndDetailsId = endDetails2
            });

            // Planned for other document -> must stay Planned
            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = Guid.NewGuid(),
                MissionId = Guid.NewGuid(),
                From = new DateOnly(2026, 2, 3),
                To = null,
                Status = MissionAssignmentStatus.Planned,
                SourceStartDocumentId = otherDocId,
                SourceStartDetailsId = Guid.NewGuid()
            });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyPostedCombatTaskDocumentAsync(docId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();

        var promoted = await db2.MissionAssignments.AsNoTracking()
            .Where(x => x.SourceStartDocumentId == docId || x.SourceEndDocumentId == docId)
            .ToListAsync();

        Assert.Equal(2, promoted.Count);
        Assert.All(promoted, x => Assert.Equal(MissionAssignmentStatus.Committed, x.Status));

        var other = await db2.MissionAssignments.AsNoTracking()
            .Where(x => x.SourceStartDocumentId == otherDocId)
            .SingleAsync();

        Assert.Equal(MissionAssignmentStatus.Planned, other.Status);
    }

    [Fact]
    public async Task ApplyPostedCombatTaskDocumentAsync_throws_when_documentId_empty()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.ApplyPostedCombatTaskDocumentAsync(Guid.Empty));
    }

    //======================================================================
    // Reads: GetActiveByMissionAsync (existing + includePlanned coverage)
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
                    Status = MissionAssignmentStatus.Committed,
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
                    Status = MissionAssignmentStatus.Committed,
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                },
                // closed -> must NOT return (To != null)
                new MissionAssignment
                {
                    Id = Guid.NewGuid(),
                    PersonId = pClosed,
                    MissionId = missionId,
                    From = new DateOnly(2026, 2, 1),
                    To = new DateOnly(2026, 2, 3),
                    Status = MissionAssignmentStatus.Committed,
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
                    Status = MissionAssignmentStatus.Committed,
                    SourceStartDocumentId = Guid.NewGuid(),
                    SourceStartDetailsId = Guid.NewGuid()
                });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var res = await repo.GetActiveByMissionAsync(missionId, onDate);

        Assert.Equal(2, res.Count);

        Assert.Equal(p1, res[0].PersonId);
        Assert.Equal(new DateOnly(2026, 2, 1), res[0].From);
        Assert.Equal("1111111111", res[0].Rnokpp);
        Assert.Equal("Alpha", res[0].FullName);

        Assert.Equal(p2, res[1].PersonId);
        Assert.Equal(new DateOnly(2026, 2, 2), res[1].From);
        Assert.Equal("2222222222", res[1].Rnokpp);
        Assert.Equal("Bravo", res[1].FullName);
    }

    [Fact]
    public async Task GetActiveByMissionAsync_includePlanned_true_returns_planned_open_ended()
    {
        await using var tdb = new SqliteTestDb();

        var missionId = Guid.NewGuid();
        var onDate = new DateOnly(2026, 2, 7);
        var p1 = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.PersonRead.Add(NewPersonRead(p1, "1111111111", "Alpha", "A"));

            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = p1,
                MissionId = missionId,
                From = new DateOnly(2026, 2, 1),
                To = null,
                Status = MissionAssignmentStatus.Planned,
                SourceStartDocumentId = Guid.NewGuid(),
                SourceStartDetailsId = Guid.NewGuid()
            });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var none = await repo.GetActiveByMissionAsync(missionId, onDate, includePlanned: false);
        Assert.Empty(none);

        var planned = await repo.GetActiveByMissionAsync(missionId, onDate, includePlanned: true);
        Assert.Single(planned);
        Assert.Equal(p1, planned[0].PersonId);
    }

    //======================================================================
    // Reads: GetActiveForPersonAsync / GetPersonAssignmentsAsync
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
                    Status = MissionAssignmentStatus.Committed,
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
                    Status = MissionAssignmentStatus.Committed,
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
        Assert.Equal(MissionAssignmentStatus.Committed, active.Status);
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
                    Status = MissionAssignmentStatus.Committed,
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
                    Status = MissionAssignmentStatus.Committed,
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
    // Reads: GetFreePersonForMissionsAsync (+ includePlanned coverage)
    //======================================================================

    [Fact]
    public async Task GetFreePersonForMissionsAsync_throws_when_onDate_default()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetFreePersonForMissionsAsync(default));
    }

    [Fact]
    public async Task GetFreePersonForMissionsAsync_returns_all_when_no_assignments_cover_date()
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
    public async Task GetFreePersonForMissionsAsync_includePlanned_true_excludes_planned_busy_person()
    {
        await using var tdb = new SqliteTestDb();

        var onDate = new DateOnly(2026, 2, 7);

        var pFree = Guid.NewGuid();
        var pPlanned = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.PersonRead.AddRange(
                NewPersonRead(pFree, "1111111111", "Free", "F"),
                NewPersonRead(pPlanned, "2222222222", "Planned", "P"));

            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = pPlanned,
                MissionId = missionId,
                From = new DateOnly(2026, 2, 1),
                To = null,
                Status = MissionAssignmentStatus.Planned,
                SourceStartDocumentId = Guid.NewGuid(),
                SourceStartDetailsId = Guid.NewGuid()
            });

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var ignorePlanned = await repo.GetFreePersonForMissionsAsync(onDate, includePlanned: false);
        Assert.Equal(2, ignorePlanned.Count); // planned не блокує

        var includePlanned = await repo.GetFreePersonForMissionsAsync(onDate, includePlanned: true);
        Assert.Single(includePlanned);
        Assert.Equal(pFree, includePlanned[0].PersonId);
    }
}
