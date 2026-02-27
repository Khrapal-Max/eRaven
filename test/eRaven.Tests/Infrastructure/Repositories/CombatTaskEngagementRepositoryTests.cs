//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEngagementRepositoryTests
//-----------------------------------------------------------------------------


using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class CombatTaskEngagementRepositoryTests
{
    private static async Task SeedDocumentAsync(
        IDbContextFactory<AppDbContext> factory,
        Guid documentId,
        DocumentStatus status,
        DateOnly recordedAt)
    {
        using var db = factory.CreateDbContext();
        db.CombatTaskDocuments.Add(new CombatTaskDocument
        {
            Id = documentId,
            Status = status,
            OrderTitle = "Order",
            RecordedAt = recordedAt,
            CreatedBy = "seed",
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<List<MissionAssignment>> LoadAssignmentsAsync(IDbContextFactory<AppDbContext> factory, Guid personId)
    {
        using var db = factory.CreateDbContext();
        return await db.MissionAssignments
            .AsNoTracking()
            .Where(x => x.PersonId == personId)
            .OrderBy(x => x.From)
            .ToListAsync();
    }

    [Fact]
    public async Task ApplyMissionFactsAsync_Throws_WhenDocumentCanceled()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(tdb.Factory);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        await SeedDocumentAsync(tdb.Factory, documentId, DocumentStatus.Canceled, new DateOnly(2026, 2, 1));

        var details = new List<CombatTaskDetails>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Kind = CombatTaskDetailsKind.Start,
                EffectiveAt = new DateOnly(2026, 2, 1),
                PersonId = personId,
                FullName = "P"
            }
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ApplyMissionFactsAsync(documentId, missionId, details, "author", DateTime.UtcNow));

        Assert.Contains("Документ", ex.Message);
    }

    [Fact]
    public async Task ApplyMissionFactsAsync_StartOnly_CreatesOpenAssignment()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(tdb.Factory);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var startDetailsId = Guid.NewGuid();
        var from = new DateOnly(2026, 2, 1);
        var nowUtc = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        await SeedDocumentAsync(tdb.Factory, documentId, DocumentStatus.Active, from);

        var affected = await repo.ApplyMissionFactsAsync(
            documentId,
            missionId,
            [
                new CombatTaskDetails
                {
                    Id = startDetailsId,
                    Kind = CombatTaskDetailsKind.Start,
                    EffectiveAt = from,
                    PersonId = personId,
                    FullName = "P"
                }
            ],
            author: "author",
            nowUtc: nowUtc);

        Assert.Single(affected);
        Assert.Equal(personId, affected[0]);

        var rows = await LoadAssignmentsAsync(tdb.Factory, personId);
        var a = Assert.Single(rows);

        Assert.Equal(personId, a.PersonId);
        Assert.Equal(missionId, a.MissionId);
        Assert.Equal(from, a.From);
        Assert.Null(a.To);

        Assert.Equal(documentId, a.SourceStartDocumentId);
        Assert.Equal(startDetailsId, a.SourceStartDetailsId);
        Assert.Null(a.SourceEndDocumentId);
        Assert.Null(a.SourceEndDetailsId);

        Assert.Equal("author", a.UpdatedBy);
        Assert.Equal(nowUtc, a.UpdatedAtUtc);
    }

    [Fact]
    public async Task ApplyMissionFactsAsync_StartAndEnd_CreatesClosedAssignment()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(tdb.Factory);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var startId = Guid.NewGuid();
        var endId = Guid.NewGuid();

        var from = new DateOnly(2026, 2, 1);
        var endInclusive = new DateOnly(2026, 2, 3);
        var expectedTo = endInclusive.AddDays(1); // half-open

        var nowUtc = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);

        await SeedDocumentAsync(tdb.Factory, documentId, DocumentStatus.Active, from);

        await repo.ApplyMissionFactsAsync(
            documentId,
            missionId,
            [
                new CombatTaskDetails { Id = startId, Kind = CombatTaskDetailsKind.Start, EffectiveAt = from, PersonId = personId, FullName = "P" },
                new CombatTaskDetails { Id = endId, Kind = CombatTaskDetailsKind.End, EffectiveAt = endInclusive, PersonId = personId, FullName = "P" },
            ],
            author: "author",
            nowUtc: nowUtc);

        var rows = await LoadAssignmentsAsync(tdb.Factory, personId);
        var a = Assert.Single(rows);

        Assert.Equal(from, a.From);
        Assert.Equal(expectedTo, a.To);
        Assert.Equal(documentId, a.SourceStartDocumentId);
        Assert.Equal(startId, a.SourceStartDetailsId);
        Assert.Equal(documentId, a.SourceEndDocumentId);
        Assert.Equal(endId, a.SourceEndDetailsId);
    }

    [Fact]
    public async Task ApplyMissionFactsAsync_ReplaceAll_RemovesAssignments_WhenPersonRemoved()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(tdb.Factory);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var from = new DateOnly(2026, 2, 1);

        await SeedDocumentAsync(tdb.Factory, documentId, DocumentStatus.Active, from);

        await repo.ApplyMissionFactsAsync(
            documentId,
            missionId,
            [
                new CombatTaskDetails { Id = Guid.NewGuid(), Kind = CombatTaskDetailsKind.Start, EffectiveAt = from, PersonId = personId, FullName = "P" },
            ],
            "author",
            DateTime.UtcNow);

        // Replace-all with empty details => compensate person and remove assignment
        var affected = await repo.ApplyMissionFactsAsync(
            documentId,
            missionId,
            [],
            "author",
            DateTime.UtcNow);

        Assert.Contains(personId, affected);

        var rows = await LoadAssignmentsAsync(tdb.Factory, personId);
        Assert.Empty(rows);
    }

    [Fact]
    public async Task ApplyMissionFactsAsync_EditStart_CompensatesPreviousStart_AndAppliesNewStart()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(tdb.Factory);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var start1Id = Guid.NewGuid();
        var start2Id = Guid.NewGuid();

        var from1 = new DateOnly(2026, 2, 1);
        var from2 = new DateOnly(2026, 2, 5);

        await SeedDocumentAsync(tdb.Factory, documentId, DocumentStatus.Active, from1);

        await repo.ApplyMissionFactsAsync(
            documentId,
            missionId,
            [
                new CombatTaskDetails { Id = start1Id, Kind = CombatTaskDetailsKind.Start, EffectiveAt = from1, PersonId = personId, FullName = "P" },
            ],
            "author",
            DateTime.UtcNow);

        await repo.ApplyMissionFactsAsync(
            documentId,
            missionId,
            [
                new CombatTaskDetails { Id = start2Id, Kind = CombatTaskDetailsKind.Start, EffectiveAt = from2, PersonId = personId, FullName = "P" },
            ],
            "author",
            DateTime.UtcNow);

        var rows = await LoadAssignmentsAsync(tdb.Factory, personId);
        var a = Assert.Single(rows);

        Assert.Equal(from2, a.From);
        Assert.Null(a.To);
        Assert.Equal(start2Id, a.SourceStartDetailsId);
    }

    [Fact]
    public async Task ApplyMissionFactsAsync_EndOnly_ClosesOpenIntervalStartedByOtherDocument()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(tdb.Factory);

        var documentId = Guid.NewGuid();
        var otherDocId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var openId = Guid.NewGuid();
        var otherStartDetailsId = Guid.NewGuid();
        var endDetailsId = Guid.NewGuid();

        var from = new DateOnly(2026, 2, 1);
        var endInclusive = new DateOnly(2026, 2, 10);
        var expectedTo = endInclusive.AddDays(1);

        await SeedDocumentAsync(tdb.Factory, documentId, DocumentStatus.Active, from);

        // Seed open assignment started by another doc
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = openId,
                PersonId = personId,
                MissionId = missionId,
                From = from,
                To = null,
                SourceStartDocumentId = otherDocId,
                SourceStartDetailsId = otherStartDetailsId,
                SourceEndDocumentId = null,
                SourceEndDetailsId = null,
                UpdatedBy = "seed",
                UpdatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        await repo.ApplyMissionFactsAsync(
            documentId,
            missionId,
            [
                new CombatTaskDetails { Id = endDetailsId, Kind = CombatTaskDetailsKind.End, EffectiveAt = endInclusive, PersonId = personId, FullName = "P" },
            ],
            "author",
            DateTime.UtcNow);

        var rows = await LoadAssignmentsAsync(tdb.Factory, personId);
        var a = Assert.Single(rows);

        Assert.Equal(openId, a.Id);
        Assert.Equal(from, a.From);
        Assert.Equal(expectedTo, a.To);
        Assert.Equal(otherDocId, a.SourceStartDocumentId);
        Assert.Equal(otherStartDetailsId, a.SourceStartDetailsId);
        Assert.Equal(documentId, a.SourceEndDocumentId);
        Assert.Equal(endDetailsId, a.SourceEndDetailsId);
    }

    [Fact]
    public async Task CancelMissionFactsAsync_RemovesStartedByDocument_AndReopensEndedByDocument()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(tdb.Factory);

        var documentId = Guid.NewGuid();
        var otherDocId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var personStarted = Guid.NewGuid();
        var personEnded = Guid.NewGuid();

        var from = new DateOnly(2026, 2, 1);
        var endInclusive = new DateOnly(2026, 2, 3);
        var toExclusive = endInclusive.AddDays(1);

        await SeedDocumentAsync(tdb.Factory, documentId, DocumentStatus.Active, from);

        var startedAssignmentId = Guid.NewGuid();
        var endedAssignmentId = Guid.NewGuid();

        using (var db = tdb.Factory.CreateDbContext())
        {
            // Started by doc => should be removed
            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = startedAssignmentId,
                PersonId = personStarted,
                MissionId = missionId,
                From = from,
                To = null,
                SourceStartDocumentId = documentId,
                SourceStartDetailsId = Guid.NewGuid(),
                SourceEndDocumentId = null,
                SourceEndDetailsId = null,
                UpdatedBy = "seed",
                UpdatedAtUtc = DateTime.UtcNow,
            });

            // Started by other doc, ended by doc => should be reopened
            db.MissionAssignments.Add(new MissionAssignment
            {
                Id = endedAssignmentId,
                PersonId = personEnded,
                MissionId = missionId,
                From = from,
                To = toExclusive,
                SourceStartDocumentId = otherDocId,
                SourceStartDetailsId = Guid.NewGuid(),
                SourceEndDocumentId = documentId,
                SourceEndDetailsId = Guid.NewGuid(),
                UpdatedBy = "seed",
                UpdatedAtUtc = DateTime.UtcNow,
            });

            await db.SaveChangesAsync();
        }

        var affected = await repo.CancelMissionFactsAsync(documentId, missionId, "author", DateTime.UtcNow);

        Assert.Equal(2, affected.Count);
        Assert.Contains(personStarted, affected);
        Assert.Contains(personEnded, affected);

        var startedRows = await LoadAssignmentsAsync(tdb.Factory, personStarted);
        Assert.Empty(startedRows);

        var endedRows = await LoadAssignmentsAsync(tdb.Factory, personEnded);
        var reopened = Assert.Single(endedRows);
        Assert.Null(reopened.To);
        Assert.Null(reopened.SourceEndDocumentId);
        Assert.Null(reopened.SourceEndDetailsId);
    }
}
