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

/// <summary>
/// Тести для <see cref="CombatTaskEngagementRepository"/>.
///
/// <para>
/// Репозиторій застосовує факти Start/End з документа до матеріалізованих інтервалів
/// <see cref="MissionAssignment"/> через агрегат <see cref="eRaven.Domain.Aggregates.PersonTaskEngagementAggregate"/>.
/// </para>
///
/// <para>
/// Ключовий контракт:
/// <list type="bullet">
/// <item><description>інтервали мають семантику half-open <c>[From..To)</c>;</description></item>
/// <item><description>для однієї особи може існувати лише один open-ended інтервал (<c>To == null</c>) серед усіх місій;</description></item>
/// <item><description><b>replace-all</b> для пари <c>(documentId, missionId)</c>: якщо особа не передана в <c>details</c>,
/// репозиторій компенсує документ для цієї особи по місії;</description></item>
/// <item><description>скасований документ (<see cref="DocumentStatus.Canceled"/>) не може застосовувати нові факти.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class CombatTaskEngagementRepositoryTests
{
    //======================================================================
    // Guards
    //======================================================================

    /// <summary>
    /// ApplyMissionFactsAsync забороняє застосування фактів для скасованого документа.
    /// </summary>
    [Fact]
    public async Task ApplyMissionFactsAsync_Throws_WhenDocumentCanceled()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(testDb.Factory);

        var docId = Guid.Parse("00000000-0000-0000-0000-000000010001");
        var missionId = Guid.Parse("00000000-0000-0000-0000-000000020001");
        var personId = Guid.Parse("00000000-0000-0000-0000-000000030001");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocument(db, docId, DocumentStatus.Canceled);
            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ApplyMissionFactsAsync(
                documentId: docId,
                missionId: missionId,
                details:
                [
                    NewDetail(Guid.Parse("00000000-0000-0000-0000-000000040001"), CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), personId)
                ],
                author: "author",
                nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc)));

        Assert.Contains("скасовано", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    //======================================================================
    // Apply: Start
    //======================================================================

    /// <summary>
    /// Start-only створює open-ended інтервал (<c>To == null</c>) та повертає affectedPersons.
    /// </summary>
    [Fact]
    public async Task ApplyMissionFactsAsync_StartOnly_CreatesOpenAssignment()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(testDb.Factory);

        var docId = Guid.Parse("00000000-0000-0000-0000-000000010011");
        var missionId = Guid.Parse("00000000-0000-0000-0000-000000020011");
        var personId = Guid.Parse("00000000-0000-0000-0000-000000030011");
        var startId = Guid.Parse("00000000-0000-0000-0000-000000040011");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocument(db, docId, DocumentStatus.Active);
            await db.SaveChangesAsync();
        }

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var affected = await repo.ApplyMissionFactsAsync(
            documentId: docId,
            missionId: missionId,
            details: [NewDetail(startId, CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), personId)],
            author: "author",
            nowUtc: now);

        Assert.Single(affected);
        Assert.Equal(personId, affected[0]);

        await using var db2 = await testDb.Factory.CreateDbContextAsync();
        var row = await db2.MissionAssignments.SingleAsync();

        Assert.Equal(personId, row.PersonId);
        Assert.Equal(missionId, row.MissionId);
        Assert.Equal(new DateOnly(2026, 02, 10), row.From);
        Assert.Null(row.To);
        Assert.Equal(docId, row.SourceStartDocumentId);
        Assert.Equal(startId, row.SourceStartDetailsId);
        Assert.Null(row.SourceEndDocumentId);
        Assert.Null(row.SourceEndDetailsId);
        Assert.Equal("author", row.UpdatedBy);
        Assert.Equal(now, row.UpdatedAtUtc);
    }

    /// <summary>
    /// Start+End в одному виклику створює bounded інтервал і виставляє <c>To = EndInclusive + 1</c>.
    /// Беремо найпізніший End серед <c>details</c>.
    /// </summary>
    [Fact]
    public async Task ApplyMissionFactsAsync_StartWithEnd_CreatesBoundedAssignment_ToExclusive()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(testDb.Factory);

        var docId = Guid.Parse("00000000-0000-0000-0000-000000010021");
        var missionId = Guid.Parse("00000000-0000-0000-0000-000000020021");
        var personId = Guid.Parse("00000000-0000-0000-0000-000000030021");

        var startId = Guid.Parse("00000000-0000-0000-0000-000000040021");
        var endEarlyId = Guid.Parse("00000000-0000-0000-0000-000000040022");
        var endLateId = Guid.Parse("00000000-0000-0000-0000-000000040023");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocument(db, docId, DocumentStatus.Active);
            await db.SaveChangesAsync();
        }

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        await repo.ApplyMissionFactsAsync(
            documentId: docId,
            missionId: missionId,
            details:
            [
                NewDetail(startId, CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), personId),
                NewDetail(endEarlyId, CombatTaskDetailsKind.End, new DateOnly(2026, 02, 11), personId),
                NewDetail(endLateId, CombatTaskDetailsKind.End, new DateOnly(2026, 02, 12), personId)
            ],
            author: "author",
            nowUtc: now);

        await using var db2 = await testDb.Factory.CreateDbContextAsync();
        var row = await db2.MissionAssignments.SingleAsync();

        Assert.Equal(new DateOnly(2026, 02, 10), row.From);
        Assert.Equal(new DateOnly(2026, 02, 13), row.To); // 12 + 1
        Assert.Equal(docId, row.SourceStartDocumentId);
        Assert.Equal(startId, row.SourceStartDetailsId);
        Assert.Equal(docId, row.SourceEndDocumentId);
        Assert.Equal(endLateId, row.SourceEndDetailsId);
    }

    //======================================================================
    // Apply: End
    //======================================================================

    /// <summary>
    /// End-only закриває open-ended інтервал, який був відкритий іншим документом.
    /// </summary>
    [Fact]
    public async Task ApplyMissionFactsAsync_EndOnly_ClosesOpenIntervalStartedByOtherDocument()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(testDb.Factory);

        var docA = Guid.Parse("00000000-0000-0000-0000-000000010031");
        var docB = Guid.Parse("00000000-0000-0000-0000-000000010032");
        var missionId = Guid.Parse("00000000-0000-0000-0000-000000020031");
        var personId = Guid.Parse("00000000-0000-0000-0000-000000030031");

        var startDetailsId = Guid.Parse("00000000-0000-0000-0000-000000040031");
        var endDetailsId = Guid.Parse("00000000-0000-0000-0000-000000040032");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocument(db, docA, DocumentStatus.Active);
            SeedDocument(db, docB, DocumentStatus.Active);

            // existing open assignment started by docA
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000050031"),
                personId: personId,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docA,
                startDetailsId: startDetailsId));

            await db.SaveChangesAsync();
        }

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        await repo.ApplyMissionFactsAsync(
            documentId: docB,
            missionId: missionId,
            details: [NewDetail(endDetailsId, CombatTaskDetailsKind.End, new DateOnly(2026, 02, 10), personId)],
            author: "closer",
            nowUtc: now);

        await using var db2 = await testDb.Factory.CreateDbContextAsync();
        var row = await db2.MissionAssignments.SingleAsync();

        Assert.Equal(new DateOnly(2026, 02, 01), row.From);
        Assert.Equal(new DateOnly(2026, 02, 11), row.To); // endInclusive 10 -> toExclusive 11
        Assert.Equal(docA, row.SourceStartDocumentId);
        Assert.Equal(startDetailsId, row.SourceStartDetailsId);
        Assert.Equal(docB, row.SourceEndDocumentId);
        Assert.Equal(endDetailsId, row.SourceEndDetailsId);
        Assert.Equal("closer", row.UpdatedBy);
        Assert.Equal(now, row.UpdatedAtUtc);
    }

    /// <summary>
    /// End-only без існуючого open-ended інтервалу створює fallback одноденний інтервал <c>[end..end+1)</c>.
    /// </summary>
    [Fact]
    public async Task ApplyMissionFactsAsync_EndOnly_WhenNoOpenInterval_CreatesOneDayAssignment()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(testDb.Factory);

        var docId = Guid.Parse("00000000-0000-0000-0000-000000010041");
        var missionId = Guid.Parse("00000000-0000-0000-0000-000000020041");
        var personId = Guid.Parse("00000000-0000-0000-0000-000000030041");
        var endDetailsId = Guid.Parse("00000000-0000-0000-0000-000000040041");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocument(db, docId, DocumentStatus.Active);
            await db.SaveChangesAsync();
        }

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        await repo.ApplyMissionFactsAsync(
            documentId: docId,
            missionId: missionId,
            details: [NewDetail(endDetailsId, CombatTaskDetailsKind.End, new DateOnly(2026, 02, 10), personId)],
            author: "closer",
            nowUtc: now);

        await using var db2 = await testDb.Factory.CreateDbContextAsync();
        var row = await db2.MissionAssignments.SingleAsync();

        Assert.Equal(personId, row.PersonId);
        Assert.Equal(missionId, row.MissionId);
        Assert.Equal(new DateOnly(2026, 02, 10), row.From);
        Assert.Equal(new DateOnly(2026, 02, 11), row.To);
        Assert.Equal(docId, row.SourceStartDocumentId);
        Assert.Equal(endDetailsId, row.SourceStartDetailsId);
        Assert.Equal(docId, row.SourceEndDocumentId);
        Assert.Equal(endDetailsId, row.SourceEndDetailsId);
    }

    //======================================================================
    // Replace-all semantics
    //======================================================================

    /// <summary>
    /// Replace-all для (document, mission): якщо особу не передано в details, документ компенсується
    /// для цієї особи по місії (інтервал видаляється / розкривається).
    /// </summary>
    [Fact]
    public async Task ApplyMissionFactsAsync_ReplaceAll_CompensatesMissingPersons()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(testDb.Factory);

        var docId = Guid.Parse("00000000-0000-0000-0000-000000010051");
        var missionId = Guid.Parse("00000000-0000-0000-0000-000000020051");

        var p1 = Guid.Parse("00000000-0000-0000-0000-000000030051");
        var p2 = Guid.Parse("00000000-0000-0000-0000-000000030052");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocument(db, docId, DocumentStatus.Active);
            await db.SaveChangesAsync();
        }

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        // 1) initial apply: p1 + p2
        var affected1 = await repo.ApplyMissionFactsAsync(
            documentId: docId,
            missionId: missionId,
            details: new[]
            {
                NewDetail(Guid.Parse("00000000-0000-0000-0000-000000040051"), CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), p1),
                NewDetail(Guid.Parse("00000000-0000-0000-0000-000000040052"), CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), p2)
            },
            author: "author",
            nowUtc: now);

        Assert.Equal(2, affected1.Count);
        Assert.Equal(p1, affected1[0]);
        Assert.Equal(p2, affected1[1]);

        // 2) update snapshot: keep only p2 (p1 is missing -> should be compensated)
        var affected2 = await repo.ApplyMissionFactsAsync(
            documentId: docId,
            missionId: missionId,
            details:
            [
                NewDetail(Guid.Parse("00000000-0000-0000-0000-000000040053"), CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), p2)
            ],
            author: "author",
            nowUtc: now.AddMinutes(1));

        // affected must include both: previous + current
        Assert.Equal(2, affected2.Count);
        Assert.Equal(p1, affected2[0]);
        Assert.Equal(p2, affected2[1]);

        await using var db2 = await testDb.Factory.CreateDbContextAsync();
        var rows = await db2.MissionAssignments.AsNoTracking().OrderBy(x => x.PersonId).ToListAsync();

        Assert.Single(rows);
        Assert.Equal(p2, rows[0].PersonId);
        Assert.Equal(docId, rows[0].SourceStartDocumentId);
    }

    //======================================================================
    // Cancel
    //======================================================================

    /// <summary>
    /// CancelMissionFactsAsync:
    /// <list type="bullet">
    /// <item><description>видаляє інтервали, які документ стартував;</description></item>
    /// <item><description>розкриває (To = null) інтервали, які документ закрив.</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task CancelMissionFactsAsync_RemovesStartedAndReopensEnded()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(testDb.Factory);

        var docA = Guid.Parse("00000000-0000-0000-0000-000000010061");
        var docB = Guid.Parse("00000000-0000-0000-0000-000000010062");
        var missionId = Guid.Parse("00000000-0000-0000-0000-000000020061");

        var pStartedByB = Guid.Parse("00000000-0000-0000-0000-000000030061");
        var pEndedByB = Guid.Parse("00000000-0000-0000-0000-000000030062");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocument(db, docA, DocumentStatus.Active);
            SeedDocument(db, docB, DocumentStatus.Active);
            await db.SaveChangesAsync();
        }

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        // Person 2: open interval started by docA, then ended by docB
        await using (var dbSeed = await testDb.Factory.CreateDbContextAsync())
        {
            dbSeed.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000050061"),
                personId: pEndedByB,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docA,
                startDetailsId: Guid.Parse("00000000-0000-0000-0000-000000040062")));

            await dbSeed.SaveChangesAsync();
        }

        // Apply docB facts in one snapshot (replace-all semantics for docB+mission).
        // If we applied in two separate calls and the second call omitted pStartedByB,
        // the repository would compensate pStartedByB automatically.
        await repo.ApplyMissionFactsAsync(
            documentId: docB,
            missionId: missionId,
            details:
            [
                NewDetail(Guid.Parse("00000000-0000-0000-0000-000000040061"), CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), pStartedByB),
                NewDetail(Guid.Parse("00000000-0000-0000-0000-000000040063"), CombatTaskDetailsKind.End, new DateOnly(2026, 02, 10), pEndedByB)
            ],
            author: "closer",
            nowUtc: now.AddMinutes(1));

        // Act: cancel docB facts for mission
        var affected = await repo.CancelMissionFactsAsync(
            documentId: docB,
            missionId: missionId,
            author: "auditor",
            nowUtc: now.AddMinutes(2));

        Assert.Equal(2, affected.Count);
        Assert.Equal(pStartedByB, affected[0]);
        Assert.Equal(pEndedByB, affected[1]);

        await using var db2 = await testDb.Factory.CreateDbContextAsync();

        // pStartedByB assignment must be removed
        Assert.False(await db2.MissionAssignments.AnyAsync(x => x.PersonId == pStartedByB));

        // pEndedByB assignment must be reopened
        var reopened = await db2.MissionAssignments.SingleAsync(x => x.PersonId == pEndedByB);
        Assert.Null(reopened.To);
        Assert.Null(reopened.SourceEndDocumentId);
        Assert.Null(reopened.SourceEndDetailsId);
        Assert.Equal("auditor", reopened.UpdatedBy);
        Assert.Equal(now.AddMinutes(2), reopened.UpdatedAtUtc);
    }

    //======================================================================
    // Invariants
    //======================================================================

    /// <summary>
    /// Якщо у особи вже є активний open-ended інтервал, Start для будь-якої місії має бути заборонений.
    /// </summary>
    [Fact]
    public async Task ApplyMissionFactsAsync_Start_Throws_WhenPersonAlreadyHasOpenAssignment()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskEngagementRepository(testDb.Factory);

        var docId = Guid.Parse("00000000-0000-0000-0000-000000010071");
        var missionId = Guid.Parse("00000000-0000-0000-0000-000000020071");
        var otherMissionId = Guid.Parse("00000000-0000-0000-0000-000000020072");
        var personId = Guid.Parse("00000000-0000-0000-0000-000000030071");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocument(db, docId, DocumentStatus.Active);

            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000050071"),
                personId: personId,
                missionId: otherMissionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: Guid.Parse("00000000-0000-0000-0000-000000010072"),
                startDetailsId: Guid.Parse("00000000-0000-0000-0000-000000040071")));

            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ApplyMissionFactsAsync(
                documentId: docId,
                missionId: missionId,
                details: [NewDetail(Guid.Parse("00000000-0000-0000-0000-000000040072"), CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), personId)],
                author: "author",
                nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc)));

        Assert.Contains("active", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static void SeedDocument(AppDbContext db, Guid id, DocumentStatus status)
    {
        var createdAt = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        db.CombatTaskDocuments.Add(new CombatTaskDocument
        {
            Id = id,
            Status = status,
            OrderTitle = $"DOC-{id:N}"[..Math.Min(128, (4 + 32))],
            Description = null,
            RecordedAt = new DateOnly(2026, 02, 10),
            CanceledReason = status == DocumentStatus.Canceled ? "X" : null,
            CreatedBy = "seed",
            CreatedAtUtc = createdAt,
            CanceledBy = status == DocumentStatus.Canceled ? "seed" : null,
            CanceledAtUtc = status == DocumentStatus.Canceled ? createdAt.AddMinutes(1) : null
        });
    }

    private static CombatTaskDetails NewDetail(Guid id, CombatTaskDetailsKind kind, DateOnly effectiveAt, Guid personId)
        => new()
        {
            Id = id,
            Kind = kind,
            EffectiveAt = effectiveAt,
            PersonId = personId,
            Rnokpp = "X",
            FullName = "Test",
            Rank = null,
            Position = null,
            Weapon = null,
            Callsign = null
        };

    private static MissionAssignment NewAssignment(
        Guid id,
        Guid personId,
        Guid missionId,
        DateOnly from,
        DateOnly? to,
        Guid startDocId,
        Guid startDetailsId)
        => new()
        {
            Id = id,
            PersonId = personId,
            MissionId = missionId,
            From = from,
            To = to,
            SourceStartDocumentId = startDocId,
            SourceStartDetailsId = startDetailsId,
            SourceEndDocumentId = null,
            SourceEndDetailsId = null,
            UpdatedBy = "seed",
            UpdatedAtUtc = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc)
        };
}
