//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskMissionAssignmentQueryRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class CombatTaskMissionAssignmentQueryRepositoryTests
{
    [Fact]
    public async Task GetActiveMissionPersonsAsync_ReturnsActiveOnDate_HalfOpen()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        // fixed guids to verify ordering
        var p1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var p2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var docId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        await SeedMissionAsync(testDb, missionId);
        await SeedDocumentAsync(testDb, docId, DocumentStatus.Active);

        // closed: [2026-02-01..2026-02-05)
        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: p1,
            missionId: missionId,
            from: new DateOnly(2026, 2, 1),
            toExclusive: new DateOnly(2026, 2, 5),
            startDocId: docId,
            startDetailsId: Guid.NewGuid(),
            endDocId: docId,
            endDetailsId: Guid.NewGuid());

        // open: [2026-02-03..)
        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: p2,
            missionId: missionId,
            from: new DateOnly(2026, 2, 3),
            toExclusive: null,
            startDocId: docId,
            startDetailsId: Guid.NewGuid(),
            endDocId: null,
            endDetailsId: null);

        var on_0402 = await repo.GetActiveMissionPersonsAsync(missionId, new DateOnly(2026, 2, 4));
        Assert.Equal([p1, p2], on_0402);

        var on_0502 = await repo.GetActiveMissionPersonsAsync(missionId, new DateOnly(2026, 2, 5));
        Assert.Equal([p2], on_0502); // p1 ended at 2026-02-05 (exclusive)

        var on_3101 = await repo.GetActiveMissionPersonsAsync(missionId, new DateOnly(2026, 1, 31));
        Assert.Empty(on_3101);
    }

    [Fact]
    public async Task GetActiveMissionPersonsAsync_ExcludesCanceledStartDocument()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var docId = Guid.NewGuid();

        await SeedMissionAsync(testDb, missionId);
        await SeedDocumentAsync(testDb, docId, DocumentStatus.Active);

        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: personId,
            missionId: missionId,
            from: new DateOnly(2026, 2, 1),
            toExclusive: null,
            startDocId: docId,
            startDetailsId: Guid.NewGuid(),
            endDocId: null,
            endDetailsId: null);

        // sanity before cancel
        var before = await repo.GetActiveMissionPersonsAsync(missionId, new DateOnly(2026, 2, 10));
        Assert.Single(before, personId);

        // Cancel document AFTER facts were created (Apply is forbidden on canceled, but data can exist historically)
        await UpdateDocumentStatusAsync(testDb, docId, DocumentStatus.Canceled);

        var after = await repo.GetActiveMissionPersonsAsync(missionId, new DateOnly(2026, 2, 10));
        Assert.Empty(after);
    }

    [Fact]
    public async Task GetActiveMissionPersonFromDatesAsync_ReturnsMinFrom_ForActiveRows()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.NewGuid();
        var docId = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        await SeedMissionAsync(testDb, missionId);
        await SeedDocumentAsync(testDb, docId, DocumentStatus.Active);

        // Two overlapping active rows for the same person (DB allows; aggregate would prevent) —
        // the query should return Min(From).
        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: p1,
            missionId: missionId,
            from: new DateOnly(2026, 2, 1),
            toExclusive: new DateOnly(2026, 2, 10),
            startDocId: docId,
            startDetailsId: Guid.NewGuid(),
            endDocId: docId,
            endDetailsId: Guid.NewGuid());

        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: p1,
            missionId: missionId,
            from: new DateOnly(2026, 2, 3),
            toExclusive: new DateOnly(2026, 2, 7),
            startDocId: docId,
            startDetailsId: Guid.NewGuid(),
            endDocId: docId,
            endDetailsId: Guid.NewGuid());

        var dict = await repo.GetActiveMissionPersonFromDatesAsync(
            missionId,
            onDate: new DateOnly(2026, 2, 5),
            personIds: [p1, p2]);

        Assert.Single(dict);
        Assert.True(dict.TryGetValue(p1, out var from));
        Assert.Equal(new DateOnly(2026, 2, 1), from);
    }

    [Fact]
    public async Task GetActiveMissionPersonsByDocumentAsync_ReturnsOnlyPersonsStartedByDocument()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.NewGuid();
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        await SeedMissionAsync(testDb, missionId);
        await SeedDocumentAsync(testDb, doc1, DocumentStatus.Active);
        await SeedDocumentAsync(testDb, doc2, DocumentStatus.Active);

        await SeedAssignmentAsync(testDb, Guid.NewGuid(), p1, missionId, new DateOnly(2026, 2, 1), null, doc1, Guid.NewGuid(), null, null);
        await SeedAssignmentAsync(testDb, Guid.NewGuid(), p2, missionId, new DateOnly(2026, 2, 1), null, doc2, Guid.NewGuid(), null, null);

        var onDate = new DateOnly(2026, 2, 2);
        var r1 = await repo.GetActiveMissionPersonsByDocumentAsync(doc1, onDate);
        Assert.Single(r1, p1);

        // if doc becomes canceled - its persons disappear from query results
        await UpdateDocumentStatusAsync(testDb, doc1, DocumentStatus.Canceled);
        var r1_after = await repo.GetActiveMissionPersonsByDocumentAsync(doc1, onDate);
        Assert.Empty(r1_after);
    }

    [Fact]
    public async Task GetPersonsWithOpenAssignmentsAsync_ReturnsOpenOnDate_ExcludesCanceledDocs()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.NewGuid();
        var docActive = Guid.NewGuid();
        var docCanceled = Guid.NewGuid();

        var pOpenActive = Guid.NewGuid();
        var pOpenCanceled = Guid.NewGuid();
        var pClosed = Guid.NewGuid();

        await SeedMissionAsync(testDb, missionId);
        await SeedDocumentAsync(testDb, docActive, DocumentStatus.Active);
        await SeedDocumentAsync(testDb, docCanceled, DocumentStatus.Active);

        await SeedAssignmentAsync(testDb, Guid.NewGuid(), pOpenActive, missionId, new DateOnly(2026, 2, 1), null, docActive, Guid.NewGuid(), null, null);

        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: pClosed,
            missionId: missionId,
            from: new DateOnly(2026, 2, 1),
            toExclusive: new DateOnly(2026, 2, 5),
            startDocId: docActive,
            startDetailsId: Guid.NewGuid(),
            endDocId: docActive,
            endDetailsId: Guid.NewGuid());

        await SeedAssignmentAsync(testDb, Guid.NewGuid(), pOpenCanceled, missionId, new DateOnly(2026, 2, 1), null, docCanceled, Guid.NewGuid(), null, null);
        await UpdateDocumentStatusAsync(testDb, docCanceled, DocumentStatus.Canceled);

        var persons = await repo.GetPersonsWithOpenAssignmentsAsync(new DateOnly(2026, 2, 4));
        Assert.Single(persons, pOpenActive);
    }

    [Fact]
    public async Task GetOccupiedPersonsInRangeAsync_ReturnsIntersections_HalfOpen_AndExcludesCanceledDocs()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.NewGuid();
        var docActive = Guid.NewGuid();
        var docCanceled = Guid.NewGuid();

        var pClosed = Guid.NewGuid();
        var pOpen = Guid.NewGuid();
        var pCanceled = Guid.NewGuid();

        await SeedMissionAsync(testDb, missionId);
        await SeedDocumentAsync(testDb, docActive, DocumentStatus.Active);
        await SeedDocumentAsync(testDb, docCanceled, DocumentStatus.Active);

        // closed [2/1..2/5)
        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: pClosed,
            missionId: missionId,
            from: new DateOnly(2026, 2, 1),
            toExclusive: new DateOnly(2026, 2, 5),
            startDocId: docActive,
            startDetailsId: Guid.NewGuid(),
            endDocId: docActive,
            endDetailsId: Guid.NewGuid());

        // open [2/10..)
        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: pOpen,
            missionId: missionId,
            from: new DateOnly(2026, 2, 10),
            toExclusive: null,
            startDocId: docActive,
            startDetailsId: Guid.NewGuid(),
            endDocId: null,
            endDetailsId: null);

        // open [2/1..) but started by a doc that later gets canceled
        await SeedAssignmentAsync(
            testDb,
            id: Guid.NewGuid(),
            personId: pCanceled,
            missionId: missionId,
            from: new DateOnly(2026, 2, 1),
            toExclusive: null,
            startDocId: docCanceled,
            startDetailsId: Guid.NewGuid(),
            endDocId: null,
            endDetailsId: null);
        await UpdateDocumentStatusAsync(testDb, docCanceled, DocumentStatus.Canceled);

        // intersects: [2/4..2/5) hits closed
        var r1 = await repo.GetOccupiedPersonsInRangeAsync(new DateOnly(2026, 2, 4), new DateOnly(2026, 2, 5));
        Assert.Single(r1, pClosed);

        // half-open: [2/5..2/10) hits nobody (closed ended at 2/5, open starts at 2/10)
        var r2 = await repo.GetOccupiedPersonsInRangeAsync(new DateOnly(2026, 2, 5), new DateOnly(2026, 2, 10));
        Assert.Empty(r2);

        // open intersects: [2/9..2/11) includes pOpen
        var r3 = await repo.GetOccupiedPersonsInRangeAsync(new DateOnly(2026, 2, 9), new DateOnly(2026, 2, 11));
        Assert.Single(r3, pOpen);

        // invalid range => empty
        var r4 = await repo.GetOccupiedPersonsInRangeAsync(new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 10));
        Assert.Empty(r4);
    }

    [Fact]
    public async Task Guards_ThrowOnInvalidInputs()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveMissionPersonsAsync(Guid.Empty, new DateOnly(2026, 2, 1)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetPersonsWithOpenAssignmentsAsync(default));
    }

    //-------------------------------------------------------------------------

    private static async Task SeedMissionAsync(SqliteTestDb testDb, Guid missionId)
    {
        await using var db = testDb.Factory.CreateDbContext();

        if (await db.Missions.AnyAsync(x => x.Id == missionId))
            return;

        db.Missions.Add(new Mission
        {
            Id = missionId,
            PositionArea = "A",
            Target = "T",
            MissionMode = MissionMode.Day,
            CreatedAt = new DateOnly(2026, 1, 1),
        });

        await db.SaveChangesAsync();
    }

    private static async Task SeedDocumentAsync(SqliteTestDb testDb, Guid docId, DocumentStatus status)
    {
        await using var db = testDb.Factory.CreateDbContext();

        if (await db.CombatTaskDocuments.AnyAsync(x => x.Id == docId))
            return;

        db.CombatTaskDocuments.Add(new CombatTaskDocument
        {
            Id = docId,
            Status = status,
            OrderTitle = $"Doc {docId:N}",
            Description = "desc",
            RecordedAt = new DateOnly(2026, 2, 1),
            CreatedBy = "test",
            CreatedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 2, 1, 10, 0, 0), DateTimeKind.Utc),
            UpdatedBy = "test",
            UpdatedAtUtc = DateTime.SpecifyKind(new DateTime(2026, 2, 1, 10, 0, 0), DateTimeKind.Utc),
        });

        await db.SaveChangesAsync();
    }

    private static async Task UpdateDocumentStatusAsync(SqliteTestDb testDb, Guid docId, DocumentStatus status)
    {
        await using var db = testDb.Factory.CreateDbContext();

        var doc = await db.CombatTaskDocuments.SingleAsync(x => x.Id == docId);
        doc.Status = status;
        doc.UpdatedBy = "test";
        doc.UpdatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        await db.SaveChangesAsync();
    }

    private static async Task SeedAssignmentAsync(
        SqliteTestDb testDb,
        Guid id,
        Guid personId,
        Guid missionId,
        DateOnly from,
        DateOnly? toExclusive,
        Guid startDocId,
        Guid startDetailsId,
        Guid? endDocId,
        Guid? endDetailsId)
    {
        await using var db = testDb.Factory.CreateDbContext();

        db.MissionAssignments.Add(new MissionAssignment
        {
            Id = id,
            PersonId = personId,
            MissionId = missionId,
            From = from,
            To = toExclusive,
            SourceStartDocumentId = startDocId,
            SourceStartDetailsId = startDetailsId,
            SourceEndDocumentId = endDocId,
            SourceEndDetailsId = endDetailsId,
            UpdatedAtUtc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            UpdatedBy = "test",
        });

        await db.SaveChangesAsync();
    }
}
