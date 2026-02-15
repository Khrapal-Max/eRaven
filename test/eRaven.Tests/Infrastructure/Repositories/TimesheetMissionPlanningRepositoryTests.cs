//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetMissionPlanningRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetMissionPlanningRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 02, 15, 10, 0, 0, DateTimeKind.Utc);

    // ======================================================================
    // GetFreePersonForMissionsAsync
    // ======================================================================

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetFreePersonForMissionsAsync кидає коли onDate=default")]
    public async Task GetFreePersonForMissionsAsync_throws_when_onDate_default()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetFreePersonForMissionsAsync(default));
    }

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetFreePersonForMissionsAsync повертає тільки тих, хто в табелі та не зайнятий активним TaskSpan")]
    public async Task GetFreePersonForMissionsAsync_returns_only_in_timesheet_and_not_busy()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var onDate = new DateOnly(2026, 02, 10);

        var p1 = Guid.NewGuid(); // in timesheet, free
        var p2 = Guid.NewGuid(); // in timesheet, busy (draft)
        var p3 = Guid.NewGuid(); // NOT in timesheet on date
        var p4 = Guid.NewGuid(); // in timesheet, has canceled span => still free

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var t3 = Guid.NewGuid();
        var t4 = Guid.NewGuid();

        var m1 = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.PersonRead.AddRange(
                NewPerson(p1, "0000000001", "Alpha A"),
                NewPerson(p2, "0000000002", "Bravo B"),
                NewPerson(p3, "0000000003", "Charlie C"),
                NewPerson(p4, "0000000004", "Delta D")
            );

            // active episodes for p1, p2, p4
            db.TimeSheets.AddRange(
                NewEpisode(t1, p1, openedAt: new DateOnly(2026, 02, 01), closedAt: null),
                NewEpisode(t2, p2, openedAt: new DateOnly(2026, 02, 01), closedAt: null),
                NewEpisode(t4, p4, openedAt: new DateOnly(2026, 02, 01), closedAt: null),

                // closed episode for p3 (not active on onDate)
                NewEpisode(t3, p3, openedAt: new DateOnly(2026, 01, 01), closedAt: new DateOnly(2026, 02, 05))
            );

            // p2 busy (active draft span)
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: t2,
                personId: p2,
                documentId: Guid.NewGuid(),
                missionId: m1,
                from: new DateOnly(2026, 02, 05),
                to: null,
                status: DocumentStatus.Draft));

            // p4 has canceled span (should NOT make busy)
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: t4,
                personId: p4,
                documentId: Guid.NewGuid(),
                missionId: m1,
                from: new DateOnly(2026, 02, 05),
                to: null,
                status: DocumentStatus.Canceled));

            await db.SaveChangesAsync();
        }

        var free = await repo.GetFreePersonForMissionsAsync(onDate);

        Assert.Equal(2, free.Count);

        // order: FullName, then Rnokpp
        Assert.Equal(p1, free[0].PersonId);
        Assert.Equal("Alpha A", free[0].FullName);

        Assert.Equal(p4, free[1].PersonId);
        Assert.Equal("Delta D", free[1].FullName);
    }

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetFreePersonForMissionsAsync не вважає busy якщо span поза датою (ToDate < onDate)")]
    public async Task GetFreePersonForMissionsAsync_span_outside_date_does_not_block()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var onDate = new DateOnly(2026, 02, 10);

        var personId = Guid.NewGuid();
        var timesheetId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.PersonRead.Add(NewPerson(personId, "0000000010", "Zulu Z"));

            db.TimeSheets.Add(NewEpisode(timesheetId, personId,
                openedAt: new DateOnly(2026, 02, 01),
                closedAt: null));

            // span ends BEFORE onDate => NOT busy
            db.TimesheetTaskSpans.Add(NewSpan(
                timesheetId: timesheetId,
                personId: personId,
                documentId: Guid.NewGuid(),
                missionId: missionId,
                from: new DateOnly(2026, 02, 05),
                to: new DateOnly(2026, 02, 08),
                status: DocumentStatus.Posted));

            await db.SaveChangesAsync();
        }

        var free = await repo.GetFreePersonForMissionsAsync(onDate);

        Assert.Single(free);
        Assert.Equal(personId, free[0].PersonId);
    }

    // ======================================================================
    // GetActiveByMissionAsync
    // ======================================================================

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetActiveByMissionAsync кидає коли missionId=empty або onDate=default")]
    public async Task GetActiveByMissionAsync_throws_on_bad_args()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveByMissionAsync(Guid.Empty, new DateOnly(2026, 02, 10), includeDraft: true));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveByMissionAsync(Guid.NewGuid(), default, includeDraft: true));
    }

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetActiveByMissionAsync(includeDraft=false) повертає тільки Posted")]
    public async Task GetActiveByMissionAsync_includeDraft_false_returns_posted_only()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var onDate = new DateOnly(2026, 02, 10);
        var missionId = Guid.NewGuid();

        var p1 = Guid.NewGuid(); // posted
        var p2 = Guid.NewGuid(); // draft
        var p3 = Guid.NewGuid(); // canceled

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var t3 = Guid.NewGuid();

        var dPosted = Guid.NewGuid();
        var dDraft = Guid.NewGuid();
        var dCanceled = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.PersonRead.AddRange(
                NewPerson(p1, "0000000101", "Alpha A"),
                NewPerson(p2, "0000000102", "Bravo B"),
                NewPerson(p3, "0000000103", "Charlie C")
            );

            db.TimeSheets.AddRange(
                NewEpisode(t1, p1, new DateOnly(2026, 02, 01), null),
                NewEpisode(t2, p2, new DateOnly(2026, 02, 01), null),
                NewEpisode(t3, p3, new DateOnly(2026, 02, 01), null)
            );

            db.TimesheetTaskSpans.AddRange(
                NewSpan(t1, p1, dPosted, missionId, new DateOnly(2026, 02, 09), null, DocumentStatus.Posted),
                NewSpan(t2, p2, dDraft, missionId, new DateOnly(2026, 02, 08), null, DocumentStatus.Draft),
                NewSpan(t3, p3, dCanceled, missionId, new DateOnly(2026, 02, 07), null, DocumentStatus.Canceled)
            );

            await db.SaveChangesAsync();
        }

        var active = await repo.GetActiveByMissionAsync(missionId, onDate, includeDraft: false);

        Assert.Single(active);

        Assert.Equal(dPosted, active[0].CombatTaskDocumentId);
        Assert.Equal(missionId, active[0].MissionId);
        Assert.Equal(p1, active[0].PersonId);
        Assert.Equal("Alpha A", active[0].FullName);
        Assert.Equal(new DateOnly(2026, 02, 09), active[0].From);
    }

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetActiveByMissionAsync(includeDraft=true) повертає Draft + Posted (Canceled ігнорує)")]
    public async Task GetActiveByMissionAsync_includeDraft_true_includes_draft_and_posted()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var onDate = new DateOnly(2026, 02, 10);
        var missionId = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();

        var d1 = Guid.NewGuid();
        var d2 = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.PersonRead.AddRange(
                NewPerson(p1, "0000000201", "Alpha A"),
                NewPerson(p2, "0000000202", "Bravo B")
            );

            db.TimeSheets.AddRange(
                NewEpisode(t1, p1, new DateOnly(2026, 02, 01), null),
                NewEpisode(t2, p2, new DateOnly(2026, 02, 01), null)
            );

            db.TimesheetTaskSpans.AddRange(
                NewSpan(t1, p1, d1, missionId, new DateOnly(2026, 02, 09), null, DocumentStatus.Posted),
                NewSpan(t2, p2, d2, missionId, new DateOnly(2026, 02, 08), null, DocumentStatus.Draft)
            );

            await db.SaveChangesAsync();
        }

        var active = await repo.GetActiveByMissionAsync(missionId, onDate, includeDraft: true);

        Assert.Equal(2, active.Count);
        Assert.Contains(active, x => x.PersonId == p1 && x.CombatTaskDocumentId == d1);
        Assert.Contains(active, x => x.PersonId == p2 && x.CombatTaskDocumentId == d2);
    }

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetActiveByMissionAsync бере найсвіжіший span на людину (max FromDate), та сортує по FromDate")]
    public async Task GetActiveByMissionAsync_picks_latest_span_per_person_and_orders_by_from()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var onDate = new DateOnly(2026, 02, 15);
        var missionId = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();

        var dOld = Guid.NewGuid();
        var dNew = Guid.NewGuid();
        var dP2 = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.PersonRead.AddRange(
                NewPerson(p1, "0000000301", "Bravo B"),
                NewPerson(p2, "0000000302", "Alpha A")
            );

            db.TimeSheets.AddRange(
                NewEpisode(t1, p1, new DateOnly(2026, 02, 01), null),
                NewEpisode(t2, p2, new DateOnly(2026, 02, 01), null)
            );

            // p1 has TWO spans on same mission (bad data), repo must pick later FromDate
            db.TimesheetTaskSpans.AddRange(
                NewSpan(t1, p1, dOld, missionId, new DateOnly(2026, 02, 01), null, DocumentStatus.Posted),
                NewSpan(t1, p1, dNew, missionId, new DateOnly(2026, 02, 09), null, DocumentStatus.Posted),

                // p2 FromDate earlier => should come first in result ordering by FromDate asc
                NewSpan(t2, p2, dP2, missionId, new DateOnly(2026, 02, 05), null, DocumentStatus.Posted)
            );

            await db.SaveChangesAsync();
        }

        var active = await repo.GetActiveByMissionAsync(missionId, onDate, includeDraft: false);

        Assert.Equal(2, active.Count);

        // ordered by FromDate ascending => p2 (02-05) first, then p1 (02-09)
        Assert.Equal(p2, active[0].PersonId);
        Assert.Equal(new DateOnly(2026, 02, 05), active[0].From);

        Assert.Equal(p1, active[1].PersonId);
        Assert.Equal(new DateOnly(2026, 02, 09), active[1].From);
        Assert.Equal(dNew, active[1].CombatTaskDocumentId); // latest span chosen
    }

    // ======================================================================
    // GetActiveByDocumentAsync
    // ======================================================================

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetActiveByDocumentAsync кидає коли documentId=empty або onDate=default")]
    public async Task GetActiveByDocumentAsync_throws_on_bad_args()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveByDocumentAsync(Guid.Empty, new DateOnly(2026, 02, 10), includeDraft: true));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repo.GetActiveByDocumentAsync(Guid.NewGuid(), default, includeDraft: true));
    }

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetActiveByDocumentAsync(includeDraft=false) повертає тільки Posted по конкретному документу")]
    public async Task GetActiveByDocumentAsync_includeDraft_false_filters_posted_and_document()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var onDate = new DateOnly(2026, 02, 10);
        var documentId = Guid.NewGuid();

        var mission1 = Guid.NewGuid();
        var mission2 = Guid.NewGuid();

        var p1 = Guid.NewGuid(); // posted in document
        var p2 = Guid.NewGuid(); // draft in document (excluded when includeDraft=false)
        var p3 = Guid.NewGuid(); // posted but different document (must be ignored)

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();
        var t3 = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.PersonRead.AddRange(
                NewPerson(p1, "0000000401", "Alpha A"),
                NewPerson(p2, "0000000402", "Bravo B"),
                NewPerson(p3, "0000000403", "Charlie C")
            );

            db.TimeSheets.AddRange(
                NewEpisode(t1, p1, new DateOnly(2026, 02, 01), null),
                NewEpisode(t2, p2, new DateOnly(2026, 02, 01), null),
                NewEpisode(t3, p3, new DateOnly(2026, 02, 01), null)
            );

            db.TimesheetTaskSpans.AddRange(
                NewSpan(t1, p1, documentId, mission1, new DateOnly(2026, 02, 09), null, DocumentStatus.Posted),
                NewSpan(t2, p2, documentId, mission2, new DateOnly(2026, 02, 08), null, DocumentStatus.Draft),
                NewSpan(t3, p3, Guid.NewGuid(), mission1, new DateOnly(2026, 02, 07), null, DocumentStatus.Posted)
            );

            await db.SaveChangesAsync();
        }

        var active = await repo.GetActiveByDocumentAsync(documentId, onDate, includeDraft: false);

        Assert.Single(active);
        Assert.Equal(p1, active[0].PersonId);
        Assert.Equal(documentId, active[0].CombatTaskDocumentId);
        Assert.Equal(mission1, active[0].MissionId);
    }

    [Fact(DisplayName = "TimesheetMissionPlanningRepo: GetActiveByDocumentAsync(includeDraft=true) повертає Draft + Posted по документу")]
    public async Task GetActiveByDocumentAsync_includeDraft_true_includes_draft_and_posted()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetMissionPlanningRepository(testDb.Factory);

        var onDate = new DateOnly(2026, 02, 10);
        var documentId = Guid.NewGuid();
        var mission1 = Guid.NewGuid();
        var mission2 = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var t1 = Guid.NewGuid();
        var t2 = Guid.NewGuid();

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.PersonRead.AddRange(
                NewPerson(p1, "0000000501", "Alpha A"),
                NewPerson(p2, "0000000502", "Bravo B")
            );

            db.TimeSheets.AddRange(
                NewEpisode(t1, p1, new DateOnly(2026, 02, 01), null),
                NewEpisode(t2, p2, new DateOnly(2026, 02, 01), null)
            );

            db.TimesheetTaskSpans.AddRange(
                NewSpan(t1, p1, documentId, mission1, new DateOnly(2026, 02, 09), null, DocumentStatus.Posted),
                NewSpan(t2, p2, documentId, mission2, new DateOnly(2026, 02, 08), null, DocumentStatus.Draft)
            );

            await db.SaveChangesAsync();
        }

        var active = await repo.GetActiveByDocumentAsync(documentId, onDate, includeDraft: true);

        Assert.Equal(2, active.Count);
        Assert.Contains(active, x => x.PersonId == p1 && x.MissionId == mission1 && x.CombatTaskDocumentId == documentId);
        Assert.Contains(active, x => x.PersonId == p2 && x.MissionId == mission2 && x.CombatTaskDocumentId == documentId);
    }

    // ======================================================================
    // helpers
    // ======================================================================

    private static PersonReadModel NewPerson(Guid id, string rnokpp, string fullName)
        => new()
        {
            Id = id,
            Lifecycle = PersonLifecycle.Enrolled,
            EnrollmentKind = null,
            EnrollmentReference = null,

            Rnokpp = rnokpp,
            LastName = fullName,
            FirstName = fullName,
            MiddleName = null,
            FullName = fullName,

            Rank = "Рядовий",
            PositionSort = null,
            Position = "Стрілець",
            Bzvp = null,
            Weapon = "АК",
            Callsign = null,

            EnrolledAt = new DateOnly(2026, 02, 01),
            ExcludedAt = null,

            Version = 1,
            UpdatedAtUtc = NowUtc
        };

    private static TimeSheetAggregate NewEpisode(Guid timesheetId, Guid personId, DateOnly openedAt, DateOnly? closedAt)
        => new()
        {
            Id = timesheetId,
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = closedAt,
            CreatedBy = "seed",
            CreatedAtUtc = NowUtc,
            ClosedBy = null,
            ClosedAtUtc = null
        };

    private static TimesheetTaskSpan NewSpan(
        Guid timesheetId,
        Guid personId,
        Guid documentId,
        Guid missionId,
        DateOnly from,
        DateOnly? to,
        DocumentStatus status)
        => new()
        {
            Id = Guid.NewGuid(),
            TimesheetId = timesheetId,
            PersonId = personId,
            CombatTaskDocumentId = documentId,
            MissionId = missionId,
            FromDate = from,
            ToDate = to,
            Status = status,
            ClosedByCodeId = null,
            ClosedReference = null,
            UpdatedBy = "seed",
            UpdatedAtUtc = NowUtc
        };
}
