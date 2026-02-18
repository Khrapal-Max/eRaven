//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetAggregateRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="TimesheetAggregateRepository"/>.
///
/// <para>
/// Фіксуємо контракт "документ (CombatTaskDetails) → факт у табелі (TimesheetTaskSpan)":
/// <list type="bullet">
/// <item><description>ApplyCombatTaskFactsAsync створює/оновлює span на людину в межах (documentId + missionId);</description></item>
/// <item><description>Start → відкриває інтервал (ToDate == null);</description></item>
/// <item><description>End → закриває інтервал (ToDate = End + 1 день, EXCLUSIVE);</description></item>
/// <item><description>End без Start допускається лише якщо span вже існує (беремо FromDate з факту);</description></item>
/// <item><description>CancelCombatTaskFactsAsync робить компенсацію (Status=Canceled) і проставляє reason/reference;</description></item>
/// <item><description>LoadActive/LoadOnDate повертають епізод з TaskSpans.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetAggregateRepositoryTests
{
    //======================================================================
    // ApplyCombatTaskFactsAsync
    //======================================================================

    /// <summary>
    /// ApplyCombatTaskFactsAsync — no-op коли details порожній.
    /// </summary>
    [Fact]
    public async Task ApplyCombatTaskFactsAsync_NoOp_WhenDetailsEmpty()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        await repo.ApplyCombatTaskFactsAsync(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            details: [],
            author: "tester",
            nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));

        await using var db = await testDb.Factory.CreateDbContextAsync();
        Assert.Equal(0, await db.TimesheetTaskSpans.CountAsync());
    }

    /// <summary>
    /// Start-only: створює активний span з FromDate=start, ToDate=null та snapshot з Start-рядка.
    /// </summary>
    [Fact]
    public async Task ApplyCombatTaskFactsAsync_StartOnly_CreatesOpenSpan_WithSnapshot()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, personId, openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var details = new[]
        {
            NewDetail(
                kind: CombatTaskDetailsKind.Start,
                effectiveAt: new DateOnly(2026, 02, 10),
                personId: personId,
                rnokpp: "1111111111",
                fullName: "Start Person",
                rank: "R",
                position: "P",
                weapon: "W",
                callsign: "C")
        };

        await repo.ApplyCombatTaskFactsAsync(documentId, missionId, details, "duty", now);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var ep = await db.TimeSheets.Include(x => x.TaskSpans).SingleAsync(x => x.PersonId == personId);

        Assert.Single(ep.TaskSpans);

        var span = ep.TaskSpans[0];
        Assert.Equal(ep.Id, span.TimesheetId);
        Assert.Equal(personId, span.PersonId);
        Assert.Equal(missionId, span.MissionId);
        Assert.Equal(documentId, span.OpenedByCombatTaskDocumentId);
        Assert.Null(span.ClosedByCombatTaskDocumentId);

        Assert.Equal(new DateOnly(2026, 02, 10), span.FromDate);
        Assert.Null(span.ToDate);
        Assert.Equal(DocumentStatus.Active, span.Status);

        // Snapshot from Start row
        Assert.Equal("1111111111", span.Rnokpp);
        Assert.Equal("Start Person", span.FullName);
        Assert.Equal("R", span.Rank);
        Assert.Equal("P", span.Position);
        Assert.Equal("W", span.Weapon);
        Assert.Equal("C", span.Callsign);

        Assert.Equal("duty", span.CreatedBy);
        Assert.Equal(now, span.CreatedAtUtc);
        Assert.Equal("duty", span.UpdatedBy);
        Assert.Equal(now, span.UpdatedAtUtc);
    }

    /// <summary>
    /// Start+End: створює span і закриває його (ToDate = End+1, EXCLUSIVE),
    /// ClosedByCombatTaskDocumentId = documentId.
    /// </summary>
    [Fact]
    public async Task ApplyCombatTaskFactsAsync_StartAndEnd_CreatesAndClosesSpan()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, personId, openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var details = new[]
        {
            NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), personId, "2222222222", "P", null, null, null, null),
            NewDetail(CombatTaskDetailsKind.End,   new DateOnly(2026, 02, 12), personId, "2222222222", "P", null, null, null, null),
        };

        await repo.ApplyCombatTaskFactsAsync(documentId, missionId, details, "duty", now);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var span = await db.TimesheetTaskSpans.SingleAsync(x => x.PersonId == personId);

        Assert.Equal(new DateOnly(2026, 02, 10), span.FromDate);
        Assert.Equal(new DateOnly(2026, 02, 13), span.ToDate); // End + 1 day (exclusive)
        Assert.Equal(documentId, span.ClosedByCombatTaskDocumentId);

        // Half-open check (semantic)
        Assert.True(span.IsActiveOn(new DateOnly(2026, 02, 12)));
        Assert.False(span.IsActiveOn(new DateOnly(2026, 02, 13)));
    }

    /// <summary>
    /// End-only допускається якщо span вже існує:
    /// Apply має взяти FromDate з існуючого факту і оновити snapshot з End-рядка.
    /// </summary>
    [Fact]
    public async Task ApplyCombatTaskFactsAsync_EndOnly_UsesExistingFromDate_AndUpdatesSnapshot()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var t1 = t0.AddMinutes(5);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, personId, openedAt: new DateOnly(2026, 02, 01), nowUtc: t0);

        // 1) First apply Start-only (creates open span)
        await repo.ApplyCombatTaskFactsAsync(
            documentId,
            missionId,
            [
                NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), personId, "3333333333", "StartName", null, null, null, null)
            ],
            author: "duty",
            nowUtc: t0);

        // 2) Then apply End-only (must close existing; snapshot should come from End row)
        await repo.ApplyCombatTaskFactsAsync(
            documentId,
            missionId,
            [
                NewDetail(CombatTaskDetailsKind.End, new DateOnly(2026, 02, 11), personId, "3333333333", "EndName", "R2", "P2", "W2", "C2")
            ],
            author: "duty2",
            nowUtc: t1);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var span = await db.TimesheetTaskSpans.SingleAsync(x => x.PersonId == personId);

        // FromDate preserved from existing span
        Assert.Equal(new DateOnly(2026, 02, 10), span.FromDate);

        // Closed at End+1 (exclusive)
        Assert.Equal(new DateOnly(2026, 02, 12), span.ToDate);
        Assert.Equal(documentId, span.ClosedByCombatTaskDocumentId);

        // Snapshot updated from End row
        Assert.Equal("3333333333", span.Rnokpp);
        Assert.Equal("EndName", span.FullName);
        Assert.Equal("R2", span.Rank);
        Assert.Equal("P2", span.Position);
        Assert.Equal("W2", span.Weapon);
        Assert.Equal("C2", span.Callsign);

        Assert.Equal("duty2", span.UpdatedBy);
        Assert.Equal(t1, span.UpdatedAtUtc);
    }

    /// <summary>
    /// Apply має обробляти кількох людей в одному виклику (group by PersonId).
    /// </summary>
    [Fact]
    public async Task ApplyCombatTaskFactsAsync_MultiplePersons_CreatesSpanPerPerson()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, p1, openedAt: new DateOnly(2026, 02, 01), nowUtc: now);
        await SeedEpisodeAsync(testDb, p2, openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var details = new[]
        {
            NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), p1, "4444444444", "P1", null, null, null, null),
            NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 11), p2, "5555555555", "P2", null, null, null, null),
        };

        await repo.ApplyCombatTaskFactsAsync(documentId, missionId, details, "duty", now);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var spans = await db.TimesheetTaskSpans.AsNoTracking().OrderBy(x => x.Rnokpp).ToListAsync();

        Assert.Equal(2, spans.Count);

        Assert.Equal(p1, spans[0].PersonId);
        Assert.Equal(new DateOnly(2026, 02, 10), spans[0].FromDate);

        Assert.Equal(p2, spans[1].PersonId);
        Assert.Equal(new DateOnly(2026, 02, 11), spans[1].FromDate);
    }

    /// <summary>
    /// Apply кидає помилку, якщо активний епізод для людини на refDate відсутній.
    /// </summary>
    [Fact]
    public async Task ApplyCombatTaskFactsAsync_Throws_WhenNoEpisodeOnRefDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ApplyCombatTaskFactsAsync(
                documentId: Guid.NewGuid(),
                missionId: Guid.NewGuid(),
                details:
                [
                    NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), Guid.NewGuid(), "6666666666", "X", null, null, null, null)
                ],
                author: "duty",
                nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc)));

        Assert.Contains("episode not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    //======================================================================
    // CancelCombatTaskFactsAsync
    //======================================================================

    /// <summary>
    /// CancelCombatTaskFactsAsync робить компенсацію:
    /// Status=Canceled + ClosedByCodeId + trimmed reference.
    /// </summary>
    [Fact]
    public async Task CancelCombatTaskFactsAsync_CancelsSpan_AndSetsReasonAndReference()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var t1 = t0.AddMinutes(3);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, personId, openedAt: new DateOnly(2026, 02, 01), nowUtc: t0);

        // Create span
        await repo.ApplyCombatTaskFactsAsync(
            documentId,
            missionId,
            [
                NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), personId, "7777777777", "P", null, null, null, null)
            ],
            author: "duty",
            nowUtc: t0);

        var reasonCodeId = Guid.NewGuid();

        await repo.CancelCombatTaskFactsAsync(
            documentId: documentId,
            missionId: missionId,
            reasonCodeId: reasonCodeId,
            reference: "  F200  ",
            author: "auditor",
            nowUtc: t1);

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var span = await db.TimesheetTaskSpans.SingleAsync(x => x.PersonId == personId);

        Assert.Equal(DocumentStatus.Canceled, span.Status);
        Assert.Equal(reasonCodeId, span.ClosedByCodeId);
        Assert.Equal("F200", span.ClosedReference);
        Assert.Equal("auditor", span.UpdatedBy);
        Assert.Equal(t1, span.UpdatedAtUtc);
    }

    /// <summary>
    /// CancelCombatTaskFactsAsync — no-op, якщо відповідних фактів не знайдено.
    /// </summary>
    [Fact]
    public async Task CancelCombatTaskFactsAsync_NoOp_WhenNoMatchingSpans()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        // Should not throw
        await repo.CancelCombatTaskFactsAsync(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            reasonCodeId: Guid.NewGuid(),
            reference: "X",
            author: "auditor",
            nowUtc: new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc));
    }

    /// <summary>
    /// CancelCombatTaskFactsAsync не повинен повторно "скасовувати" вже canceled span (вибірка фільтрує Status!=Canceled).
    /// </summary>
    [Fact]
    public async Task CancelCombatTaskFactsAsync_NoOp_WhenAlreadyCanceled()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, personId, openedAt: new DateOnly(2026, 02, 01), nowUtc: t0);

        // Create + cancel once
        await repo.ApplyCombatTaskFactsAsync(
            documentId,
            missionId,
            [NewDetail(CombatTaskDetailsKind.Start, new DateOnly(2026, 02, 10), personId, "8888888888", "P", null, null, null, null)],
            "duty",
            t0);

        var reason1 = Guid.NewGuid();
        await repo.CancelCombatTaskFactsAsync(documentId, missionId, reason1, "A", "aud1", t0.AddMinutes(1));

        // Second cancel should be a no-op (no matching spans due to Status!=Canceled in query)
        var reason2 = Guid.NewGuid();
        await repo.CancelCombatTaskFactsAsync(documentId, missionId, reason2, "B", "aud2", t0.AddMinutes(2));

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var span = await db.TimesheetTaskSpans.SingleAsync(x => x.PersonId == personId);

        Assert.Equal(DocumentStatus.Canceled, span.Status);
        Assert.Equal(reason1, span.ClosedByCodeId);
        Assert.Equal("A", span.ClosedReference);
        Assert.Equal("aud1", span.UpdatedBy);
    }

    //======================================================================
    // LoadActiveAsync / LoadOnDateForUpdateAsync
    //======================================================================

    /// <summary>
    /// LoadActiveAsync повертає активний епізод з TaskSpans.
    /// </summary>
    [Fact]
    public async Task LoadActiveAsync_ReturnsEpisode_WithTaskSpans()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var personId = Guid.NewGuid();

        var epId = await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), now);

        // Add one span to prove Include works
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var ep = await db.TimeSheets.Include(x => x.TaskSpans).SingleAsync(x => x.Id == epId);

            ep.UpsertTask(
                documentId: Guid.NewGuid(),
                missionId: Guid.NewGuid(),
                from: new DateOnly(2026, 02, 10),
                toExclusive: null,
                rnokpp: "9999999999",
                fullName: "P",
                rank: null,
                position: null,
                weapon: null,
                callsign: null,
                author: "seed",
                nowUtc: now);

            await db.SaveChangesAsync();
        }

        var loaded = await repo.LoadActiveAsync(personId);

        Assert.Equal(personId, loaded.PersonId);
        Assert.Single(loaded.TaskSpans);
    }

    /// <summary>
    /// LoadOnDateForUpdateAsync повертає епізод, який покриває дату onDate.
    /// </summary>
    [Fact]
    public async Task LoadOnDateForUpdateAsync_ReturnsEpisodeCoveringDate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetAggregateRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var personId = Guid.NewGuid();

        await SeedEpisodeAsync(testDb, personId, new DateOnly(2026, 02, 01), now);

        var loaded = await repo.LoadOnDateForUpdateAsync(personId, new DateOnly(2026, 02, 10));
        Assert.Equal(personId, loaded.PersonId);
        Assert.True(loaded.IsActiveOn(new DateOnly(2026, 02, 10)));
    }

    /// <summary>
    /// Тест на поведінку: doc1 start → doc2 end (без concurrency і без дублювання span)
    /// </summary>
    [Fact]
    public async Task ApplyFacts_StartInDoc1_EndInDoc2_ClosesExistingSpan_AndStoresClosingDocId()
    {
        // arrange
        var personId = Guid.NewGuid();
        var missionId = Guid.NewGuid();
        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();

        var openedAt = new DateOnly(2026, 1, 1);
        var start = new DateOnly(2026, 1, 5);
        var end = new DateOnly(2026, 1, 7);

        await using var testDb = new SqliteTestDb();
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();

            db.TimeSheets.Add(new TimeSheetAggregate
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                OpenedAt = openedAt,
                ClosedAt = null,
                CreatedBy = "seed",
                CreatedAtUtc = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetAggregateRepository(testDb.Factory);

        // act 1: start by doc1
        await repo.ApplyCombatTaskFactsAsync(
            documentId: doc1,
            missionId: missionId,
            details:
            [
                new CombatTaskDetails
                {
                    PersonId = personId,
                    Kind = CombatTaskDetailsKind.Start,
                    EffectiveAt = start,
                    Rnokpp = "123",
                    FullName = "Test Person",
                    Rank = "R",
                    Position = "P",
                    Weapon = "W",
                    Callsign = "C"
                }
            ],
            author: "tester",
            nowUtc: DateTime.UtcNow);

        // act 2: end by doc2 (no start in details)
        await repo.ApplyCombatTaskFactsAsync(
            documentId: doc2,
            missionId: missionId,
            details:
            [
                new CombatTaskDetails
                {
                    PersonId = personId,
                    Kind = CombatTaskDetailsKind.End,
                    EffectiveAt = end,
                    Rnokpp = "123",
                    FullName = "Test Person"
                }
            ],
            author: "tester",
            nowUtc: DateTime.UtcNow);

        // assert
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var spans = await db.TimesheetTaskSpans
                .AsNoTracking()
                .Where(s => s.PersonId == personId && s.MissionId == missionId)
                .ToListAsync();

            Assert.Single(spans);

            var s = spans[0];
            Assert.Equal(doc1, s.OpenedByCombatTaskDocumentId);
            Assert.Equal(doc2, s.ClosedByCombatTaskDocumentId);
            Assert.Equal(start, s.FromDate);
            Assert.Equal(end.AddDays(1), s.ToDate); // exclusive
            Assert.NotEqual(DocumentStatus.Canceled, s.Status);
        }
    }

    //======================================================================
    // Helpers
    //======================================================================

    /// <summary>
    /// Створює мінімально валідний активний епізод табеля для людини.
    /// </summary>
    private static async Task<Guid> SeedEpisodeAsync(SqliteTestDb testDb, Guid personId, DateOnly openedAt, DateTime nowUtc)
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

        await using var db = await testDb.Factory.CreateDbContextAsync();
        db.TimeSheets.Add(ep);
        await db.SaveChangesAsync();

        return ep.Id;
    }

    /// <summary>
    /// Утиліта створення snapshot-рядка завдання (CombatTaskDetails).
    /// CombatTaskId не використовується репозиторієм Apply*, але поле заповнюємо як валідне.
    /// </summary>
    private static CombatTaskDetails NewDetail(
        CombatTaskDetailsKind kind,
        DateOnly effectiveAt,
        Guid personId,
        string rnokpp,
        string fullName,
        string? rank,
        string? position,
        string? weapon,
        string? callsign)
        => new()
        {
            Id = Guid.NewGuid(),
            CombatTaskId = Guid.NewGuid(),
            Kind = kind,
            EffectiveAt = effectiveAt,
            PersonId = personId,
            Rnokpp = rnokpp,
            FullName = fullName,
            Rank = rank,
            Position = position,
            Weapon = weapon,
            Callsign = callsign
        };
}
