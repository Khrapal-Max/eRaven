//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for <see cref="TimesheetAggregateRepository"/> task-control sync:
/// CombatTaskDetails -> MissionAssignment -> Timesheet entries (30/100).
/// </summary>
public sealed class TimesheetAggregateRepository_TaskControl_Tests
{
    private const string Author = "test";
    private static readonly DateTime NowUtc = new(2026, 02, 21, 12, 0, 0, DateTimeKind.Utc);

    private static async Task SeedCodesAsync(SqliteTestDb testDb, CancellationToken ct = default)
    {
        await using var db = testDb.Factory.CreateDbContext();
        await TimesheetPolicySeed.EnsureSeedAsync(db, ct);
    }

    private static async Task<Guid> GetCodeIdAsync(SqliteTestDb testDb, string code, CancellationToken ct = default)
    {
        await using var db = testDb.Factory.CreateDbContext();
        return await db.TimesheetCodes
            .AsNoTracking()
            .Where(x => x.Code == code)
            .Select(x => x.Id)
            .SingleAsync(ct);
    }

    private static async Task SeedEpisodeAsync(SqliteTestDb testDb, Guid personId, DateOnly openedAt, CancellationToken ct = default)
    {
        var readyId = await GetCodeIdAsync(testDb, TimesheetSystemCodes.ReadyToCombatTask, ct);

        var ep = new TimeSheetAggregate
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            OpenedAt = openedAt,
            ClosedAt = null,
            CreatedBy = Author,
            CreatedAtUtc = NowUtc
        };

        // Baseline 30 from OpenedAt.
        ep.AddTimesheetEntry(readyId, openedAt, reference: string.Empty, author: Author, nowUtc: NowUtc);

        await using var db = testDb.Factory.CreateDbContext();
        db.TimeSheets.Add(ep);
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDocumentAsync(
        SqliteTestDb testDb,
        Guid documentId,
        string orderTitle,
        DocumentStatus status = DocumentStatus.Active,
        DateOnly? recordedAt = null,
        CancellationToken ct = default)
    {
        await using var db = testDb.Factory.CreateDbContext();

        db.CombatTaskDocuments.Add(new CombatTaskDocument
        {
            Id = documentId,
            Status = status,
            OrderTitle = orderTitle,
            RecordedAt = recordedAt ?? new DateOnly(2026, 02, 01),
            CreatedBy = Author,
            CreatedAtUtc = NowUtc
        });

        await db.SaveChangesAsync(ct);
    }

    private sealed record EntrySnapshot(DateOnly From, DateOnly? To, string Code, string? Reference);

    private static async Task<List<EntrySnapshot>> LoadEntriesAsync(SqliteTestDb testDb, Guid personId, CancellationToken ct = default)
    {
        await using var db = testDb.Factory.CreateDbContext();

        var ep = await db.TimeSheets
            .AsNoTracking()
            .Include(x => x.Entries)
            .SingleAsync(x => x.PersonId == personId && x.ClosedAt == null, ct);

        var codeMap = await db.TimesheetCodes
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Code, ct);

        return [.. ep.Entries
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.From)
            .Select(e => new EntrySnapshot(
                From: e.From,
                To: e.To,
                Code: codeMap[e.TimesheetCodeDefinitionId],
                Reference: e.Reference))];
    }

    private static void AssertNormalized(List<EntrySnapshot> entries)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            if (i < entries.Count - 1)
                Assert.Equal(entries[i + 1].From, entries[i].To);
            else
                Assert.Null(entries[i].To);
        }
    }

    private static void AssertNoTaskEntries(List<EntrySnapshot> entries)
    {
        Assert.DoesNotContain(entries, x => x.Code == TimesheetSystemCodes.DoesTheCombatTask);
    }

    private static async Task<List<MissionAssignment>> LoadAssignmentsStartedByAsync(
        SqliteTestDb testDb,
        Guid sourceStartDocumentId,
        Guid missionId,
        CancellationToken ct = default)
    {
        await using var db = testDb.Factory.CreateDbContext();
        return await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.SourceStartDocumentId == sourceStartDocumentId && x.MissionId == missionId)
            .OrderBy(x => x.From)
            .ToListAsync(ct);
    }

    private static async Task<List<MissionAssignment>> LoadAssignmentsEndedByAsync(
        SqliteTestDb testDb,
        Guid sourceEndDocumentId,
        Guid missionId,
        CancellationToken ct = default)
    {
        await using var db = testDb.Factory.CreateDbContext();
        return await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.SourceEndDocumentId == sourceEndDocumentId && x.MissionId == missionId)
            .OrderBy(x => x.From)
            .ToListAsync(ct);
    }

    [Fact]
    public async Task AssignedTask_Writes100WithReference_AndSplitsFrom30()
    {
        await using var testDb = new SqliteTestDb();
        await SeedCodesAsync(testDb);

        var personId = Guid.NewGuid();
        var openedAt = new DateOnly(2026, 02, 01);
        await SeedEpisodeAsync(testDb, personId, openedAt);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        await SeedDocumentAsync(testDb, docId, orderTitle: "Order-1");

        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var start = new DateOnly(2026, 02, 10);
        var details = new List<CombatTaskDetails>
        {
            new() { Id = Guid.NewGuid(), Kind = CombatTaskDetailsKind.Start, EffectiveAt = start, PersonId = personId }
        };

        await repo.ApplyCombatTaskFactsAsync(docId, "Order-1", missionId, details, Author, NowUtc);

        var assigns = await LoadAssignmentsStartedByAsync(testDb, docId, missionId);
        Assert.Single(assigns);
        Assert.Equal(personId, assigns[0].PersonId);
        Assert.Equal(start, assigns[0].From);
        Assert.Null(assigns[0].To);
        Assert.Equal(docId, assigns[0].SourceStartDocumentId);
        Assert.Null(assigns[0].SourceEndDocumentId);

        var entries = await LoadEntriesAsync(testDb, personId);
        Assert.Equal(2, entries.Count);

        Assert.Equal(openedAt, entries[0].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[0].Code);

        Assert.Equal(start, entries[1].From);
        Assert.Equal(TimesheetSystemCodes.DoesTheCombatTask, entries[1].Code);
        Assert.Equal("Order-1", entries[1].Reference);

        AssertNormalized(entries);
    }

    [Fact]
    public async Task EndTask_AfterStart_Writes30AtEndPlus1()
    {
        await using var testDb = new SqliteTestDb();
        await SeedCodesAsync(testDb);

        var personId = Guid.NewGuid();
        var openedAt = new DateOnly(2026, 02, 01);
        await SeedEpisodeAsync(testDb, personId, openedAt);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        await SeedDocumentAsync(testDb, docId, orderTitle: "Order-1");

        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var start = new DateOnly(2026, 02, 10);
        var end = new DateOnly(2026, 02, 15);

        // 1) Document initially contains only Start
        var startDetailsId = Guid.NewGuid();
        await repo.ApplyCombatTaskFactsAsync(docId, "Order-1", missionId,
            [new() { Id = startDetailsId, Kind = CombatTaskDetailsKind.Start, EffectiveAt = start, PersonId = personId }],
            Author, NowUtc);

        // 2) Document updated later: contains Start + End (document content is authoritative snapshot)
        var endDetailsId = Guid.NewGuid();
        await repo.ApplyCombatTaskFactsAsync(docId, "Order-1", missionId,
            [
                new() { Id = startDetailsId, Kind = CombatTaskDetailsKind.Start, EffectiveAt = start, PersonId = personId },
                new() { Id = endDetailsId, Kind = CombatTaskDetailsKind.End, EffectiveAt = end, PersonId = personId }
            ],
            Author, NowUtc);

        var assigns = await LoadAssignmentsStartedByAsync(testDb, docId, missionId);
        Assert.Single(assigns);
        Assert.Equal(start, assigns[0].From);
        Assert.Equal(end.AddDays(1), assigns[0].To);
        Assert.Equal(docId, assigns[0].SourceStartDocumentId);
        Assert.Equal(docId, assigns[0].SourceEndDocumentId);
        Assert.Equal(endDetailsId, assigns[0].SourceEndDetailsId);

        var expectedBackTo30 = end.AddDays(1);

        var entries = await LoadEntriesAsync(testDb, personId);

        Assert.Equal(3, entries.Count);
        Assert.Equal(openedAt, entries[0].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[0].Code);

        Assert.Equal(start, entries[1].From);
        Assert.Equal(TimesheetSystemCodes.DoesTheCombatTask, entries[1].Code);
        Assert.Equal("Order-1", entries[1].Reference);

        Assert.Equal(expectedBackTo30, entries[2].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[2].Code);

        AssertNormalized(entries);
    }

    [Fact]
    public async Task EndAndAssignSameDay_UpdatesReferenceSet_OnSameCode()
    {
        await using var testDb = new SqliteTestDb();
        await SeedCodesAsync(testDb);

        var personId = Guid.NewGuid();
        var openedAt = new DateOnly(2026, 02, 01);
        await SeedEpisodeAsync(testDb, personId, openedAt);

        var missionId = Guid.NewGuid();

        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        await SeedDocumentAsync(testDb, doc1, orderTitle: "Order-1");
        await SeedDocumentAsync(testDb, doc2, orderTitle: "Order-2");

        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var start1 = new DateOnly(2026, 02, 10);
        var end1 = new DateOnly(2026, 02, 15);
        var start2 = end1; // same day

        // Doc1 contains Start+End (single authoritative snapshot)
        var start1Id = Guid.NewGuid();
        var end1Id = Guid.NewGuid();
        await repo.ApplyCombatTaskFactsAsync(doc1, "Order-1", missionId,
            [
                new() { Id = start1Id, Kind = CombatTaskDetailsKind.Start, EffectiveAt = start1, PersonId = personId },
                new() { Id = end1Id, Kind = CombatTaskDetailsKind.End, EffectiveAt = end1, PersonId = personId }
            ],
            Author, NowUtc);

        // Doc2 Start on the same day as doc1 End
        await repo.ApplyCombatTaskFactsAsync(doc2, "Order-2", missionId,
            [new() { Id = Guid.NewGuid(), Kind = CombatTaskDetailsKind.Start, EffectiveAt = start2, PersonId = personId }],
            Author, NowUtc);

        var assigns1 = await LoadAssignmentsStartedByAsync(testDb, doc1, missionId);
        Assert.Single(assigns1);
        Assert.Equal(start1, assigns1[0].From);
        Assert.Equal(end1.AddDays(1), assigns1[0].To);
        Assert.Equal(doc1, assigns1[0].SourceStartDocumentId);
        Assert.Equal(doc1, assigns1[0].SourceEndDocumentId);

        var assigns2 = await LoadAssignmentsStartedByAsync(testDb, doc2, missionId);
        Assert.Single(assigns2);
        Assert.Equal(start2, assigns2[0].From);
        Assert.Null(assigns2[0].To);
        Assert.Equal(doc2, assigns2[0].SourceStartDocumentId);
        Assert.Null(assigns2[0].SourceEndDocumentId);

        var dOverlap = start2;
        var dAfter = end1.AddDays(1);

        var entries = await LoadEntriesAsync(testDb, personId);

        // Expected change points:
        // openedAt: 30
        // start1: 100 "Order-1"
        // start2: 100 "Order-1, Order-2" (set changed)
        // dAfter: 100 "Order-2" (Order-1 removed)
        Assert.Equal(4, entries.Count);

        Assert.Equal(openedAt, entries[0].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[0].Code);

        Assert.Equal(start1, entries[1].From);
        Assert.Equal(TimesheetSystemCodes.DoesTheCombatTask, entries[1].Code);
        Assert.Equal("Order-1", entries[1].Reference);

        Assert.Equal(dOverlap, entries[2].From);
        Assert.Equal(TimesheetSystemCodes.DoesTheCombatTask, entries[2].Code);
        Assert.Equal("Order-1, Order-2", entries[2].Reference);

        Assert.Equal(dAfter, entries[3].From);
        Assert.Equal(TimesheetSystemCodes.DoesTheCombatTask, entries[3].Code);
        Assert.Equal("Order-2", entries[3].Reference);

        AssertNormalized(entries);
    }

    [Fact]
    public async Task AssignedThenCanceled_RevertsToSingle30_AndRemovesAssignments()
    {
        await using var testDb = new SqliteTestDb();
        await SeedCodesAsync(testDb);

        var personId = Guid.NewGuid();
        var openedAt = new DateOnly(2026, 02, 01);
        await SeedEpisodeAsync(testDb, personId, openedAt);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        await SeedDocumentAsync(testDb, docId, orderTitle: "Order-1");

        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var start = new DateOnly(2026, 02, 10);

        await repo.ApplyCombatTaskFactsAsync(docId, "Order-1", missionId,
            [new() { Id = Guid.NewGuid(), Kind = CombatTaskDetailsKind.Start, EffectiveAt = start, PersonId = personId }],
            Author, NowUtc);

        await repo.CancelCombatTaskFactsAsync(docId, missionId, Author, NowUtc);

        var assigns = await LoadAssignmentsStartedByAsync(testDb, docId, missionId);
        Assert.Empty(assigns);

        var entries = await LoadEntriesAsync(testDb, personId);

        // Desired invariant for cancellation: no redundant change-points.
        // Only baseline 30 from OpenedAt should remain.
        Assert.Single(entries);
        Assert.Equal(openedAt, entries[0].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[0].Code);
        AssertNormalized(entries);
        AssertNoTaskEntries(entries);
    }

    [Fact]
    public async Task EndedThenCanceled_RevertsToSingle30_AndRemovesAssignments()
    {
        await using var testDb = new SqliteTestDb();
        await SeedCodesAsync(testDb);

        var personId = Guid.NewGuid();
        var openedAt = new DateOnly(2026, 02, 01);
        await SeedEpisodeAsync(testDb, personId, openedAt);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        await SeedDocumentAsync(testDb, docId, orderTitle: "Order-1");

        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var start = new DateOnly(2026, 02, 10);
        var end = new DateOnly(2026, 02, 15);

        // 1) Document initially contains only Start
        var startId = Guid.NewGuid();
        await repo.ApplyCombatTaskFactsAsync(docId, "Order-1", missionId,
            [new() { Id = startId, Kind = CombatTaskDetailsKind.Start, EffectiveAt = start, PersonId = personId }],
            Author, NowUtc);

        // 2) Document updated later: contains Start + End
        await repo.ApplyCombatTaskFactsAsync(docId, "Order-1", missionId,
            [
                new() { Id = startId, Kind = CombatTaskDetailsKind.Start, EffectiveAt = start, PersonId = personId },
                new() { Id = Guid.NewGuid(), Kind = CombatTaskDetailsKind.End, EffectiveAt = end, PersonId = personId }
            ],
            Author, NowUtc);

        await repo.CancelCombatTaskFactsAsync(docId, missionId, Author, NowUtc);

        var assigns = await LoadAssignmentsStartedByAsync(testDb, docId, missionId);
        Assert.Empty(assigns);

        var entries = await LoadEntriesAsync(testDb, personId);

        Assert.Single(entries);
        Assert.Equal(openedAt, entries[0].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[0].Code);
        AssertNormalized(entries);
        AssertNoTaskEntries(entries);
    }

    [Fact]
    public async Task EndOnlyWithoutStart_CreatesOneDayTask_ThenBackTo30()
    {
        await using var testDb = new SqliteTestDb();
        await SeedCodesAsync(testDb);

        var personId = Guid.NewGuid();
        var openedAt = new DateOnly(2026, 02, 01);
        await SeedEpisodeAsync(testDb, personId, openedAt);

        var docId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        await SeedDocumentAsync(testDb, docId, orderTitle: "Order-1");

        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var end = new DateOnly(2026, 02, 20);

        var endDetailsId2 = Guid.NewGuid();
        await repo.ApplyCombatTaskFactsAsync(docId, "Order-1", missionId,
            [new() { Id = endDetailsId2, Kind = CombatTaskDetailsKind.End, EffectiveAt = end, PersonId = personId }],
            Author, NowUtc);

        var assigns = await LoadAssignmentsStartedByAsync(testDb, docId, missionId);
        Assert.Single(assigns);
        Assert.Equal(end, assigns[0].From);
        Assert.Equal(end.AddDays(1), assigns[0].To);
        Assert.Equal(docId, assigns[0].SourceStartDocumentId);
        Assert.Equal(docId, assigns[0].SourceEndDocumentId);
        Assert.Equal(endDetailsId2, assigns[0].SourceEndDetailsId);

        var entries = await LoadEntriesAsync(testDb, personId);
        Assert.Equal(3, entries.Count);

        Assert.Equal(openedAt, entries[0].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[0].Code);

        Assert.Equal(end, entries[1].From);
        Assert.Equal(TimesheetSystemCodes.DoesTheCombatTask, entries[1].Code);
        Assert.Equal("Order-1", entries[1].Reference);

        Assert.Equal(end.AddDays(1), entries[2].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[2].Code);

        AssertNormalized(entries);
    }

    [Fact]
    public async Task EndOnlyInClosingDocument_ClosesOpenIntervalStartedByOtherDocument()
    {
        await using var testDb = new SqliteTestDb();
        await SeedCodesAsync(testDb);

        var personId = Guid.NewGuid();
        var openedAt = new DateOnly(2026, 02, 01);
        await SeedEpisodeAsync(testDb, personId, openedAt);

        var missionId = Guid.NewGuid();
        var docStart = Guid.NewGuid();
        var docClose = Guid.NewGuid();

        await SeedDocumentAsync(testDb, docStart, orderTitle: "Order-Start");
        await SeedDocumentAsync(testDb, docClose, orderTitle: "Order-Close");

        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var start = new DateOnly(2026, 02, 10);
        var end = new DateOnly(2026, 02, 15);

        // Start is in docStart
        await repo.ApplyCombatTaskFactsAsync(docStart, "Order-Start", missionId,
            [new() { Id = Guid.NewGuid(), Kind = CombatTaskDetailsKind.Start, EffectiveAt = start, PersonId = personId }],
            Author, NowUtc);

        // End is in docClose (closing document)
        var endDetailsId = Guid.NewGuid();
        await repo.ApplyCombatTaskFactsAsync(docClose, "Order-Close", missionId,
            [new() { Id = endDetailsId, Kind = CombatTaskDetailsKind.End, EffectiveAt = end, PersonId = personId }],
            Author, NowUtc);

        // Interval remains the one started by docStart, but is closed by docClose.
        var assigns = await LoadAssignmentsStartedByAsync(testDb, docStart, missionId);
        Assert.Single(assigns);
        Assert.Equal(start, assigns[0].From);
        Assert.Equal(end.AddDays(1), assigns[0].To);
        Assert.Equal(docStart, assigns[0].SourceStartDocumentId);
        Assert.Equal(docClose, assigns[0].SourceEndDocumentId);
        Assert.Equal(endDetailsId, assigns[0].SourceEndDetailsId);

        var endedBy = await LoadAssignmentsEndedByAsync(testDb, docClose, missionId);
        Assert.Single(endedBy);
        Assert.Equal(assigns[0].Id, endedBy[0].Id);

        // Timesheet shows task code from start until end+1.
        var entries = await LoadEntriesAsync(testDb, personId);
        Assert.Equal(3, entries.Count);
        Assert.Equal(openedAt, entries[0].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[0].Code);
        Assert.Equal(start, entries[1].From);
        Assert.Equal(TimesheetSystemCodes.DoesTheCombatTask, entries[1].Code);
        Assert.Equal("Order-Start", entries[1].Reference);
        Assert.Equal(end.AddDays(1), entries[2].From);
        Assert.Equal(TimesheetSystemCodes.ReadyToCombatTask, entries[2].Code);
        AssertNormalized(entries);
    }
}
