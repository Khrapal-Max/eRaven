//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskMissionAssignmentQueryRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="CombatTaskMissionAssignmentQueryRepository"/>.
///
/// <para>
/// Репозиторій читає матеріалізовані інтервали <see cref="MissionAssignment"/> і фільтрує їх через
/// статус <see cref="CombatTaskDocument"/> (join по <c>SourceStartDocumentId</c>).
/// </para>
///
/// <para>
/// Семантика інтервалів: half-open <c>[From..To)</c>.
/// </para>
/// </summary>
public sealed class CombatTaskMissionAssignmentQueryRepositoryTests
{
    //======================================================================
    // GetActiveMissionPersonsAsync
    //======================================================================

    /// <summary>
    /// Повертає distinct+sorted PersonId, активних на дату, та виключає інтервали,
    /// відкриті документом зі статусом <see cref="DocumentStatus.Canceled"/>.
    /// </summary>
    [Fact]
    public async Task GetActiveMissionPersonsAsync_ReturnsDistinctSorted_AndExcludesCanceledStartDocs()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.Parse("00000000-0000-0000-0000-000000000101");
        var onDate = new DateOnly(2026, 02, 10);

        var docActive1 = Guid.Parse("00000000-0000-0000-0000-000000000201");
        var docActive2 = Guid.Parse("00000000-0000-0000-0000-000000000202");
        var docCanceled = Guid.Parse("00000000-0000-0000-0000-000000000299");

        var p1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var p2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var p3 = Guid.Parse("00000000-0000-0000-0000-000000000003");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocuments(db, docActive1, DocumentStatus.Active);
            SeedDocuments(db, docActive2, DocumentStatus.Active);
            SeedDocuments(db, docCanceled, DocumentStatus.Canceled);

            // p1: open interval (active) opened by active doc
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000001001"),
                personId: p1,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docActive1));

            // p1: another overlapping interval with To != null (still active) — should not duplicate output
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000001002"),
                personId: p1,
                missionId: missionId,
                from: new DateOnly(2026, 02, 05),
                to: new DateOnly(2026, 02, 15),
                startDocId: docActive2));

            // p2: active bounded interval
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000001003"),
                personId: p2,
                missionId: missionId,
                from: new DateOnly(2026, 02, 10),
                to: new DateOnly(2026, 02, 12),
                startDocId: docActive1));

            // p3: opened by canceled doc — must be excluded
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000001004"),
                personId: p3,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docCanceled));

            await db.SaveChangesAsync();
        }

        var persons = await repo.GetActiveMissionPersonsAsync(missionId, onDate);

        Assert.Equal(2, persons.Count);
        Assert.Equal(p1, persons[0]);
        Assert.Equal(p2, persons[1]);
    }

    //======================================================================
    // GetActiveMissionPersonFromDatesAsync
    //======================================================================

    /// <summary>
    /// Повертає мінімальну дату From для кожної особи, яка активна на дату.
    /// </summary>
    [Fact]
    public async Task GetActiveMissionPersonFromDatesAsync_ReturnsMinFrom_PerPerson()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.Parse("00000000-0000-0000-0000-000000000111");
        var onDate = new DateOnly(2026, 02, 10);

        var docActive1 = Guid.Parse("00000000-0000-0000-0000-000000000211");
        var docActive2 = Guid.Parse("00000000-0000-0000-0000-000000000212");

        var p1 = Guid.Parse("00000000-0000-0000-0000-000000000011");
        var p2 = Guid.Parse("00000000-0000-0000-0000-000000000012");
        var pX = Guid.Parse("00000000-0000-0000-0000-000000000013");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocuments(db, docActive1, DocumentStatus.Active);
            SeedDocuments(db, docActive2, DocumentStatus.Active);

            // Two overlapping bounded intervals for p1 (defensive scenario) — should pick the earliest From
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000002001"),
                personId: p1,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: new DateOnly(2026, 02, 20),
                startDocId: docActive1));

            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000002002"),
                personId: p1,
                missionId: missionId,
                from: new DateOnly(2026, 02, 05),
                to: new DateOnly(2026, 02, 15),
                startDocId: docActive2));

            // p2: single interval
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000002003"),
                personId: p2,
                missionId: missionId,
                from: new DateOnly(2026, 02, 10),
                to: null,
                startDocId: docActive1));

            await db.SaveChangesAsync();
        }

        var dict = await repo.GetActiveMissionPersonFromDatesAsync(
            missionId: missionId,
            onDate: onDate,
            personIds: [p1, p2, pX]);

        Assert.Equal(2, dict.Count);
        Assert.Equal(new DateOnly(2026, 02, 01), dict[p1]);
        Assert.Equal(new DateOnly(2026, 02, 10), dict[p2]);
        Assert.False(dict.ContainsKey(pX));
    }

    //======================================================================
    // GetActiveMissionPersonsByDocumentAsync
    //======================================================================

    /// <summary>
    /// Повертає людей, у яких активні інтервали відкриті конкретним документом.
    /// </summary>
    [Fact]
    public async Task GetActiveMissionPersonsByDocumentAsync_ReturnsPersonsStartedByDocumentOnly()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var missionId = Guid.Parse("00000000-0000-0000-0000-000000000121");
        var onDate = new DateOnly(2026, 02, 10);

        var docActive1 = Guid.Parse("00000000-0000-0000-0000-000000000221");
        var docActive2 = Guid.Parse("00000000-0000-0000-0000-000000000222");

        var p1 = Guid.Parse("00000000-0000-0000-0000-000000000021");
        var p2 = Guid.Parse("00000000-0000-0000-0000-000000000022");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocuments(db, docActive1, DocumentStatus.Active);
            SeedDocuments(db, docActive2, DocumentStatus.Active);

            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000003001"),
                personId: p1,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docActive1));

            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000003002"),
                personId: p2,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docActive2));

            await db.SaveChangesAsync();
        }

        var persons = await repo.GetActiveMissionPersonsByDocumentAsync(docActive1, onDate);

        Assert.Single(persons);
        Assert.Equal(p1, persons[0]);
    }

    //======================================================================
    // GetPersonsWithOpenAssignmentsAsync
    //======================================================================

    /// <summary>
    /// Повертає людей з open-ended інтервалами (To == null) на дату.
    /// </summary>
    [Fact]
    public async Task GetPersonsWithOpenAssignmentsAsync_ReturnsPersonsWithToNull_AndFiltersCanceledDocs()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var onDate = new DateOnly(2026, 02, 10);

        var missionId = Guid.Parse("00000000-0000-0000-0000-000000000131");
        var docActive = Guid.Parse("00000000-0000-0000-0000-000000000231");
        var docCanceled = Guid.Parse("00000000-0000-0000-0000-000000000239");

        var p1 = Guid.Parse("00000000-0000-0000-0000-000000000031");
        var p2 = Guid.Parse("00000000-0000-0000-0000-000000000032");
        var p3 = Guid.Parse("00000000-0000-0000-0000-000000000033");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocuments(db, docActive, DocumentStatus.Active);
            SeedDocuments(db, docCanceled, DocumentStatus.Canceled);

            // p1: open-ended active
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000004001"),
                personId: p1,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docActive));

            // p2: open-ended but starts in the future — not active on onDate
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000004002"),
                personId: p2,
                missionId: missionId,
                from: new DateOnly(2026, 02, 15),
                to: null,
                startDocId: docActive));

            // p3: open-ended but opened by canceled doc — excluded
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000004003"),
                personId: p3,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docCanceled));

            await db.SaveChangesAsync();
        }

        var persons = await repo.GetPersonsWithOpenAssignmentsAsync(onDate);

        Assert.Single(persons);
        Assert.Equal(p1, persons[0]);
    }

    //======================================================================
    // GetOccupiedPersonsInRangeAsync
    //======================================================================

    /// <summary>
    /// Повертає людей, у яких є будь-які інтервали, що перетинають діапазон <c>[from..toExclusive)</c>.
    /// </summary>
    [Fact]
    public async Task GetOccupiedPersonsInRangeAsync_ReturnsIntersectingPersons_AndFiltersCanceledDocs()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var from = new DateOnly(2026, 02, 09);
        var toExclusive = new DateOnly(2026, 02, 12);

        var missionId = Guid.Parse("00000000-0000-0000-0000-000000000141");
        var docActive = Guid.Parse("00000000-0000-0000-0000-000000000241");
        var docCanceled = Guid.Parse("00000000-0000-0000-0000-000000000249");

        var p1 = Guid.Parse("00000000-0000-0000-0000-000000000041");
        var p2 = Guid.Parse("00000000-0000-0000-0000-000000000042");
        var p3 = Guid.Parse("00000000-0000-0000-0000-000000000043");
        var p4 = Guid.Parse("00000000-0000-0000-0000-000000000044");

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            SeedDocuments(db, docActive, DocumentStatus.Active);
            SeedDocuments(db, docCanceled, DocumentStatus.Canceled);

            // p1: ends at 2026-02-10 (exclusive), still intersects [09..12)
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000005001"),
                personId: p1,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: new DateOnly(2026, 02, 10),
                startDocId: docActive));

            // p2: starts exactly at toExclusive — does NOT intersect
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000005002"),
                personId: p2,
                missionId: missionId,
                from: toExclusive,
                to: new DateOnly(2026, 02, 15),
                startDocId: docActive));

            // p3: open-ended intersects
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000005003"),
                personId: p3,
                missionId: missionId,
                from: new DateOnly(2026, 02, 05),
                to: null,
                startDocId: docActive));

            // p4: intersects, but opened by canceled doc — excluded
            db.MissionAssignments.Add(NewAssignment(
                id: Guid.Parse("00000000-0000-0000-0000-000000005004"),
                personId: p4,
                missionId: missionId,
                from: new DateOnly(2026, 02, 01),
                to: null,
                startDocId: docCanceled));

            await db.SaveChangesAsync();
        }

        var persons = await repo.GetOccupiedPersonsInRangeAsync(from, toExclusive);

        Assert.Equal(2, persons.Count);
        Assert.Equal(p1, persons[0]);
        Assert.Equal(p3, persons[1]);
    }

    /// <summary>
    /// Якщо <c>toExclusive &lt;= from</c>, метод повертає порожній список (оптимізація).
    /// </summary>
    [Fact]
    public async Task GetOccupiedPersonsInRangeAsync_ReturnsEmpty_WhenToExclusiveNotAfterFrom()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new CombatTaskMissionAssignmentQueryRepository(testDb.Factory);

        var d = new DateOnly(2026, 02, 10);
        var persons = await repo.GetOccupiedPersonsInRangeAsync(d, d);

        Assert.Empty(persons);
    }

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Seed'ить <see cref="CombatTaskDocument"/> з потрібним статусом.
    /// </summary>
    private static void SeedDocuments(AppDbContext db, Guid id, DocumentStatus status)
    {
        if (db.CombatTaskDocuments.Local.Any(x => x.Id == id) || db.CombatTaskDocuments.Any(x => x.Id == id))
            return;

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        db.CombatTaskDocuments.Add(new CombatTaskDocument
        {
            Id = id,
            Status = status,
            OrderTitle = $"Doc-{id:N}"[..Math.Min(20, $"Doc-{id:N}".Length)],
            Description = null,
            RecordedAt = new DateOnly(2026, 02, 01),
            CreatedBy = "seed",
            CreatedAtUtc = now,
            UpdatedBy = "seed",
            UpdatedAtUtc = now,
            CanceledBy = status == DocumentStatus.Canceled ? "seed" : null,
            CanceledAtUtc = status == DocumentStatus.Canceled ? now.AddMinutes(1) : null,
            CanceledReason = status == DocumentStatus.Canceled ? "X" : null
        });
    }

    /// <summary>
    /// Створює мінімально валідний <see cref="MissionAssignment"/>.
    /// </summary>
    private static MissionAssignment NewAssignment(
        Guid id,
        Guid personId,
        Guid missionId,
        DateOnly from,
        DateOnly? to,
        Guid startDocId)
        => new()
        {
            Id = id,
            PersonId = personId,
            MissionId = missionId,
            From = from,
            To = to,
            SourceStartDocumentId = startDocId,
            SourceStartDetailsId = Guid.NewGuid(),
            SourceEndDocumentId = null,
            SourceEndDetailsId = null,
            UpdatedAtUtc = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc),
            UpdatedBy = "seed"
        };
}
