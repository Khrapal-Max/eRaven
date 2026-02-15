//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAssignmentRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class MissionAssignmentRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 15, 12, 0, 0, DateTimeKind.Utc);

    //======================================================================
    // Helpers
    //======================================================================

    private static TimeSheetAggregate NewEpisode(Guid timesheetId, Guid personId, DateOnly openedAt)
        => new()
        {
            Id = timesheetId,
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = null,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddHours(-1)
        };

    private static TimesheetTaskSpan NewSpan(
        Guid spanId,
        Guid timesheetId,
        Guid personId,
        Guid documentId,
        Guid missionId,
        DateOnly from,
        DateOnly? to,
        DocumentStatus status)
        => new()
        {
            Id = spanId,
            TimesheetId = timesheetId,
            PersonId = personId,
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            FromDate = from,
            ToDate = to,
            Status = status,
            UpdatedBy = "seed",
            UpdatedAtUtc = NowUtc
        };

    private static MissionAssignment NewAssignment(
        Guid id,
        Guid documentId,
        Guid missionId,
        Guid personId,
        DateOnly from,
        DateOnly? to,
        Guid? closedByDocumentId = null)
        => new()
        {
            Id = id,
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            PersonId = personId,
            From = from,
            To = to,
            ClosedByDocumentId = closedByDocumentId
        };

    //======================================================================
    // GetPersonAssignmentsAsync
    //======================================================================

    [Fact]
    public async Task GetPersonAssignmentsAsync_throws_on_empty_person_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetPersonAssignmentsAsync(Guid.Empty, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 2)));
    }

    [Fact]
    public async Task GetPersonAssignmentsAsync_throws_when_to_before_from()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetPersonAssignmentsAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10), new DateOnly(2026, 2, 9)));
    }

    [Fact]
    public async Task GetPersonAssignmentsAsync_returns_only_overlapping_and_sorted()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();
        var otherPersonId = Guid.NewGuid();

        var m1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var m2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var doc1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var doc2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var a0 = NewAssignment(Guid.Parse("00000000-0000-0000-0000-000000000001"), doc1, m1, personId,
            from: new DateOnly(2026, 2, 1),
            to: new DateOnly(2026, 2, 5));

        var a1 = NewAssignment(Guid.Parse("00000000-0000-0000-0000-000000000002"), doc1, m2, personId,
            from: new DateOnly(2026, 2, 10),
            to: new DateOnly(2026, 2, 12));

        var a2 = NewAssignment(Guid.Parse("00000000-0000-0000-0000-000000000003"), doc2, m1, personId,
            from: new DateOnly(2026, 2, 15),
            to: null);

        var aOther = NewAssignment(Guid.Parse("00000000-0000-0000-0000-000000000004"), doc1, m1, otherPersonId,
            from: new DateOnly(2026, 2, 10),
            to: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.AddRange(a0, a1, a2, aOther);
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        // overlap window [2026-02-11..2026-02-20] -> a1 (10..12 overlaps), a2 (15..∞ overlaps); a0 excluded
        var list = await repo.GetPersonAssignmentsAsync(personId, new DateOnly(2026, 2, 11), new DateOnly(2026, 2, 20));

        Assert.Equal(2, list.Count);

        // ordering: From asc, MissionId, CombatTaskDocumentId, Id
        Assert.Equal(a1.Id, list[0].Id);
        Assert.Equal(a2.Id, list[1].Id);
    }

    //======================================================================
    // GetActiveForPersonAsync
    //======================================================================

    [Fact]
    public async Task GetActiveForPersonAsync_throws_on_empty_person_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveForPersonAsync(Guid.Empty, new DateOnly(2026, 2, 10)));
    }

    [Fact]
    public async Task GetActiveForPersonAsync_returns_null_when_none()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        var result = await repo.GetActiveForPersonAsync(Guid.NewGuid(), new DateOnly(2026, 2, 10));

        Assert.Null(result);
    }

    [Fact]
    public async Task GetActiveForPersonAsync_returns_latest_by_from_desc_then_id_desc()
    {
        await using var tdb = new SqliteTestDb();

        var personId = Guid.NewGuid();

        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();

        var mission1 = Guid.NewGuid();
        var mission2 = Guid.NewGuid();

        // Two active on 2026-02-11 with same From => pick max Guid by ThenByDescending(Id)
        var idSmall = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var idBig = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        // Older active (different mission/doc doesn't matter)
        var a0 = NewAssignment(Guid.Parse("00000000-0000-0000-0000-000000000009"), doc1, mission1, personId,
            from: new DateOnly(2026, 2, 10), to: null);

        // Same From, different (docId, missionId) pairs => не порушуємо UNIQUE(doc, mission, person)
        var a1 = NewAssignment(idSmall, doc1, mission2, personId,
            from: new DateOnly(2026, 2, 11), to: null);

        var a2 = NewAssignment(idBig, doc2, mission1, personId,
            from: new DateOnly(2026, 2, 11), to: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.AddRange(a0, a1, a2);
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var active = await repo.GetActiveForPersonAsync(personId, new DateOnly(2026, 2, 11));

        Assert.NotNull(active);
        Assert.Equal(idBig, active!.Id);
    }

    //======================================================================
    // ApplyPostedCombatTaskDocumentAsync
    //======================================================================

    [Fact]
    public async Task ApplyPostedCombatTaskDocumentAsync_throws_on_empty_document_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.ApplyPostedCombatTaskDocumentAsync(Guid.Empty));
    }

    [Fact]
    public async Task ApplyPostedCombatTaskDocumentAsync_when_no_posted_spans_should_noop()
    {
        await using var tdb = new SqliteTestDb();

        // Seed span but not Posted
        var docId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var timesheetId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(timesheetId, personId, new DateOnly(2026, 2, 1)));
            db.TimesheetTaskSpans.Add(NewSpan(
                spanId: Guid.NewGuid(),
                timesheetId: timesheetId,
                personId: personId,
                documentId: docId,
                missionId: missionId,
                from: new DateOnly(2026, 2, 10),
                to: null,
                status: DocumentStatus.Draft));
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyPostedCombatTaskDocumentAsync(docId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var any = await db2.MissionAssignments.AsNoTracking().AnyAsync(a => a.CombatTaskDocumentId == docId);
        Assert.False(any);
    }

    [Fact]
    public async Task ApplyPostedCombatTaskDocumentAsync_creates_assignments_from_posted_spans_and_is_idempotent()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var timesheetId = Guid.NewGuid();

        var from = new DateOnly(2026, 2, 10);
        var to = new DateOnly(2026, 2, 12);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimeSheets.Add(NewEpisode(timesheetId, personId, new DateOnly(2026, 2, 1)));
            db.TimesheetTaskSpans.Add(NewSpan(
                spanId: Guid.NewGuid(),
                timesheetId: timesheetId,
                personId: personId,
                documentId: docId,
                missionId: missionId,
                from: from,
                to: to,
                status: DocumentStatus.Posted));
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyPostedCombatTaskDocumentAsync(docId);
        await repo.ApplyPostedCombatTaskDocumentAsync(docId); // idempotent

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var list = await db2.MissionAssignments.AsNoTracking()
            .Where(a => a.CombatTaskDocumentId == docId)
            .ToListAsync();

        var a = Assert.Single(list);
        Assert.Equal(docId, a.CombatTaskDocumentId);
        Assert.Equal(missionId, a.MissionId);
        Assert.Equal(personId, a.PersonId);
        Assert.Equal(from, a.From);
        Assert.Equal(to, a.To);
        Assert.Null(a.ClosedByDocumentId);
    }

    [Fact]
    public async Task ApplyPostedCombatTaskDocumentAsync_updates_existing_assignment_and_does_not_reopen_or_touch_closedByDocumentId()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var timesheetId = Guid.NewGuid();

        var closedByDoc = Guid.NewGuid();

        // existing assignment already closed at 2026-02-11
        var existing = NewAssignment(
            id: Guid.NewGuid(),
            documentId: docId,
            missionId: missionId,
            personId: personId,
            from: new DateOnly(2026, 2, 9),
            to: new DateOnly(2026, 2, 11),
            closedByDocumentId: closedByDoc);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(existing);

            db.TimeSheets.Add(NewEpisode(timesheetId, personId, new DateOnly(2026, 2, 1)));

            // span says "Posted", from moved to 2026-02-10, ToDate = null (open-ended)
            // repo must update From, but must NOT clear/extend To, and must NOT touch ClosedByDocumentId
            db.TimesheetTaskSpans.Add(NewSpan(
                spanId: Guid.NewGuid(),
                timesheetId: timesheetId,
                personId: personId,
                documentId: docId,
                missionId: missionId,
                from: new DateOnly(2026, 2, 10),
                to: null,
                status: DocumentStatus.Posted));

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.ApplyPostedCombatTaskDocumentAsync(docId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.MissionAssignments.AsNoTracking()
            .SingleAsync(a => a.Id == existing.Id);

        Assert.Equal(new DateOnly(2026, 2, 10), reloaded.From);
        Assert.Equal(new DateOnly(2026, 2, 11), reloaded.To); // not reopened
        Assert.Equal(closedByDoc, reloaded.ClosedByDocumentId); // untouched
    }

    [Fact]
    public async Task ApplyPostedCombatTaskDocumentAsync_when_existing_to_is_null_should_set_to_from_span()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var timesheetId = Guid.NewGuid();

        var existing = NewAssignment(Guid.NewGuid(), docId, missionId, personId,
            from: new DateOnly(2026, 2, 10), to: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(existing);

            db.TimeSheets.Add(NewEpisode(timesheetId, personId, new DateOnly(2026, 2, 1)));
            db.TimesheetTaskSpans.Add(NewSpan(
                spanId: Guid.NewGuid(),
                timesheetId: timesheetId,
                personId: personId,
                documentId: docId,
                missionId: missionId,
                from: new DateOnly(2026, 2, 10),
                to: new DateOnly(2026, 2, 12),
                status: DocumentStatus.Posted));

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);
        await repo.ApplyPostedCombatTaskDocumentAsync(docId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.MissionAssignments.AsNoTracking()
            .SingleAsync(a => a.Id == existing.Id);

        Assert.Equal(new DateOnly(2026, 2, 12), reloaded.To);
    }

    [Fact]
    public async Task ApplyPostedCombatTaskDocumentAsync_when_span_to_is_earlier_than_existing_should_move_to_earlier()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var timesheetId = Guid.NewGuid();

        var existing = NewAssignment(Guid.NewGuid(), docId, missionId, personId,
            from: new DateOnly(2026, 2, 10),
            to: new DateOnly(2026, 2, 20));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(existing);

            db.TimeSheets.Add(NewEpisode(timesheetId, personId, new DateOnly(2026, 2, 1)));
            db.TimesheetTaskSpans.Add(NewSpan(
                spanId: Guid.NewGuid(),
                timesheetId: timesheetId,
                personId: personId,
                documentId: docId,
                missionId: missionId,
                from: new DateOnly(2026, 2, 10),
                to: new DateOnly(2026, 2, 15), // earlier
                status: DocumentStatus.Posted));

            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);
        await repo.ApplyPostedCombatTaskDocumentAsync(docId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.MissionAssignments.AsNoTracking()
            .SingleAsync(a => a.Id == existing.Id);

        Assert.Equal(new DateOnly(2026, 2, 15), reloaded.To);
    }

    //======================================================================
    // CloseAsync
    //======================================================================

    [Fact]
    public async Task CloseAsync_noops_when_assignment_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.CloseAsync(
            startDocumentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            personId: Guid.NewGuid(),
            closeAt: new DateOnly(2026, 2, 10),
            closedByDocumentId: Guid.NewGuid());

        // no throw => ok
    }

    [Fact]
    public async Task CloseAsync_throws_when_closeAt_before_from()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var a = NewAssignment(Guid.NewGuid(), docId, missionId, personId,
            from: new DateOnly(2026, 2, 10),
            to: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(a);
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.CloseAsync(docId, missionId, personId, closeAt: new DateOnly(2026, 2, 9), closedByDocumentId: null));

        Assert.Equal("closeAt must be >= From.", ex.Message);
    }

    [Fact]
    public async Task CloseAsync_sets_to_when_open_and_sets_closedByDocumentId_once()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var closeDocId = Guid.NewGuid();

        var a = NewAssignment(Guid.NewGuid(), docId, missionId, personId,
            from: new DateOnly(2026, 2, 10),
            to: null,
            closedByDocumentId: null);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(a);
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.CloseAsync(docId, missionId, personId, new DateOnly(2026, 2, 12), closeDocId);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.MissionAssignments.AsNoTracking().SingleAsync(x => x.Id == a.Id);

        Assert.Equal(new DateOnly(2026, 2, 12), reloaded.To);
        Assert.Equal(closeDocId, reloaded.ClosedByDocumentId);
    }

    [Fact]
    public async Task CloseAsync_when_to_is_after_closeAt_should_shorten()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var a = NewAssignment(Guid.NewGuid(), docId, missionId, personId,
            from: new DateOnly(2026, 2, 10),
            to: new DateOnly(2026, 2, 20));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(a);
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);
        await repo.CloseAsync(docId, missionId, personId, new DateOnly(2026, 2, 12), closedByDocumentId: null);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.MissionAssignments.AsNoTracking().SingleAsync(x => x.Id == a.Id);

        Assert.Equal(new DateOnly(2026, 2, 12), reloaded.To);
    }

    [Fact]
    public async Task CloseAsync_when_to_is_before_closeAt_should_keep_existing_to()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var a = NewAssignment(Guid.NewGuid(), docId, missionId, personId,
            from: new DateOnly(2026, 2, 10),
            to: new DateOnly(2026, 2, 11));

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(a);
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);
        await repo.CloseAsync(docId, missionId, personId, new DateOnly(2026, 2, 12), closedByDocumentId: Guid.NewGuid());

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.MissionAssignments.AsNoTracking().SingleAsync(x => x.Id == a.Id);

        Assert.Equal(new DateOnly(2026, 2, 11), reloaded.To); // keep earlier
    }

    [Fact]
    public async Task CloseAsync_does_not_overwrite_existing_closedByDocumentId()
    {
        await using var tdb = new SqliteTestDb();

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var firstCloseDoc = Guid.NewGuid();
        var secondCloseDoc = Guid.NewGuid();

        var a = NewAssignment(Guid.NewGuid(), docId, missionId, personId,
            from: new DateOnly(2026, 2, 10),
            to: null,
            closedByDocumentId: firstCloseDoc);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(a);
            await db.SaveChangesAsync();
        }

        var repo = new MissionAssignmentRepository(tdb.Factory);

        await repo.CloseAsync(docId, missionId, personId, new DateOnly(2026, 2, 12), secondCloseDoc);

        await using var db2 = await tdb.Factory.CreateDbContextAsync();
        var reloaded = await db2.MissionAssignments.AsNoTracking().SingleAsync(x => x.Id == a.Id);

        Assert.Equal(firstCloseDoc, reloaded.ClosedByDocumentId);
    }
}
