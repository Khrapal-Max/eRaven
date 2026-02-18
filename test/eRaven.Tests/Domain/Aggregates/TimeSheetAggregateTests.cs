//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregateTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Enums;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Domain.Aggregates;

/// <summary>
/// Тести для <see cref="TimeSheetAggregate"/>.
///
/// <para>
/// Мета: зафіксувати стратегічні інваріанти агрегата (TaskSpans як факт):
/// <list type="bullet">
/// <item><description>інтервал задачі — half-open: <c>[FromDate..ToDate)</c>, де <c>ToDate</c> — EXCLUSIVE;</description></item>
/// <item><description>заборонені перетини активних span’ів для однієї людини;</description></item>
/// <item><description>Close/Cancel — компенсаційні дії без видалення фактів;</description></item>
/// <item><description>EF зберігає/завантажує агрегат і snapshot TaskSpans без втрат.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimeSheetAggregateTests
{
    //======================================================================
    // Domain-only tests (fast)
    //======================================================================

    /// <summary>
    /// UpsertTask має створювати span, проставляти snapshot+audit
    /// та забезпечувати half-open семантику активності.
    /// </summary>
    [Fact]
    public void UpsertTask_CreatesSpan_WithSnapshot_AndHalfOpenSemantics()
    {
        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var ep = NewEpisode(openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        ep.UpsertTask(
            documentId: documentId,
            missionId: missionId,
            from: new DateOnly(2026, 02, 05),
            toExclusive: null,
            rnokpp: "1234567890",
            fullName: "Test Person",
            rank: "PVT",
            position: "Operator",
            weapon: "Rifle",
            callsign: "FOX",
            author: "tester",
            nowUtc: now);

        Assert.Single(ep.TaskSpans);

        var span = ep.TaskSpans[0];

        Assert.Equal(ep.Id, span.TimesheetId);
        Assert.Equal(ep.PersonId, span.PersonId);

        Assert.Equal(documentId, span.OpenedByCombatTaskDocumentId);
        Assert.Equal(missionId, span.MissionId);

        Assert.Equal(DocumentStatus.Active, span.Status);

        Assert.Equal(new DateOnly(2026, 02, 05), span.FromDate);
        Assert.Null(span.ToDate);

        // Snapshot
        Assert.Equal("1234567890", span.Rnokpp);
        Assert.Equal("Test Person", span.FullName);
        Assert.Equal("PVT", span.Rank);
        Assert.Equal("Operator", span.Position);
        Assert.Equal("Rifle", span.Weapon);
        Assert.Equal("FOX", span.Callsign);

        // Audit
        Assert.Equal("tester", span.CreatedBy);
        Assert.Equal(now, span.CreatedAtUtc);
        Assert.Equal("tester", span.UpdatedBy);
        Assert.Equal(now, span.UpdatedAtUtc);

        // Closed fields cleared by Upsert
        Assert.Null(span.ClosedByCombatTaskDocumentId);
        Assert.Null(span.ClosedByCodeId);
        Assert.Null(span.ClosedReference);

        // Half-open active semantics:
        Assert.False(span.IsActiveOn(new DateOnly(2026, 02, 04)));
        Assert.True(span.IsActiveOn(new DateOnly(2026, 02, 05)));
        Assert.True(span.IsActiveOn(new DateOnly(2026, 02, 10)));
    }

    /// <summary>
    /// UpsertTask має забороняти перетини активних span’ів для однієї особи.
    /// </summary>
    [Fact]
    public void UpsertTask_Throws_WhenOverlapsAnotherActiveSpan()
    {
        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var ep = NewEpisode(openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var doc1 = Guid.NewGuid();
        var doc2 = Guid.NewGuid();
        var m1 = Guid.NewGuid();
        var m2 = Guid.NewGuid();

        // span #1: [2026-02-05 .. 2026-02-10)
        ep.UpsertTask(
            documentId: doc1,
            missionId: m1,
            from: new DateOnly(2026, 02, 05),
            toExclusive: new DateOnly(2026, 02, 10),
            rnokpp: "1",
            fullName: "A",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            author: "tester",
            nowUtc: now);

        // span #2 overlaps: [2026-02-09 .. 2026-02-12)
        var ex = Assert.Throws<InvalidOperationException>(() =>
            ep.UpsertTask(
                documentId: doc2,
                missionId: m2,
                from: new DateOnly(2026, 02, 09),
                toExclusive: new DateOnly(2026, 02, 12),
                rnokpp: "2",
                fullName: "B",
                rank: null,
                position: null,
                weapon: null,
                callsign: null,
                author: "tester",
                nowUtc: now));

        Assert.Contains("overlap", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// CloseTask має виставляти <c>ToDate</c> як EXCLUSIVE межу
    /// та робити span неактивним на даті <c>ToDate</c>.
    /// </summary>
 /*   [Fact]
    public void CloseTask_SetsExclusiveToDate_AndMarksClosedByDocument()
    {
        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var ep = NewEpisode(openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        ep.UpsertTask(
            documentId: documentId,
            missionId: missionId,
            from: new DateOnly(2026, 02, 10),
            toExclusive: null,
            rnokpp: "123",
            fullName: "C",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            author: "tester",
            nowUtc: now);

        ep.CloseTaskByReason(
            documentId: documentId,
            missionId: missionId,
            closeAtExclusive: new DateOnly(2026, 02, 12),
            author: "tester2",
            nowUtc: now.AddMinutes(1));

        var span = ep.TaskSpans.Single();

        Assert.Equal(new DateOnly(2026, 02, 12), span.ToDate);
        Assert.Equal(documentId, span.ClosedByCombatTaskDocumentId);

        // Active on last included day (11), inactive on ToDate (12)
        Assert.True(span.IsActiveOn(new DateOnly(2026, 02, 11)));
        Assert.False(span.IsActiveOn(new DateOnly(2026, 02, 12)));

        // Updated audit changes
        Assert.Equal("tester2", span.UpdatedBy);
        Assert.Equal(now.AddMinutes(1), span.UpdatedAtUtc);
    }*/

    /// <summary>
    /// CloseTaskByReason має закривати активний span на дату <paramref name="closeAtExclusive"/>
    /// і проставляти причину (CodeId) та reference (trim).
    ///
    /// <para>
    /// ВАЖЛИВО: домен забороняє перетини активних span’ів для однієї особи,
    /// тому одночасно активним може бути лише один факт задачі.
    /// </para>
    /// </summary>
    [Fact]
    public void CloseTaskByReason_ClosesActiveSpan_AndSetsReason()
    {
        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var ep = NewEpisode(openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        // Old span (non-active on close day): [2026-02-02 .. 2026-02-05)
        ep.UpsertTask(
            documentId: Guid.NewGuid(),
            missionId: Guid.NewGuid(),
            from: new DateOnly(2026, 02, 02),
            toExclusive: new DateOnly(2026, 02, 05),
            rnokpp: "0000000001",
            fullName: "Old Span",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            author: "tester",
            nowUtc: now);

        // Active span to be closed: [2026-02-10 .. null)
        var activeDoc = Guid.NewGuid();
        var activeMission = Guid.NewGuid();

        ep.UpsertTask(
            documentId: activeDoc,
            missionId: activeMission,
            from: new DateOnly(2026, 02, 10),
            toExclusive: null,
            rnokpp: "0000000002",
            fullName: "Active Span",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            author: "tester",
            nowUtc: now);

        var reasonCodeId = Guid.NewGuid();

        ep.CloseTaskByReason(
            closeAtExclusive: new DateOnly(2026, 02, 11),
            reasonCodeId: reasonCodeId,
            reference: "  F200  ",
            author: "duty",
            nowUtc: now.AddMinutes(2));

        Assert.Equal(2, ep.TaskSpans.Count);

        var oldSpan = ep.TaskSpans.Single(x => x.FullName == "Old Span");
        var activeSpan = ep.TaskSpans.Single(x => x.FullName == "Active Span");

        // Old span not active on 2026-02-11 => unchanged
        Assert.Equal(new DateOnly(2026, 02, 05), oldSpan.ToDate);
        Assert.Null(oldSpan.ClosedByCodeId);
        Assert.Null(oldSpan.ClosedReference);

        // Active span closed at 2026-02-11 (exclusive)
        Assert.Equal(new DateOnly(2026, 02, 11), activeSpan.ToDate);
        Assert.Equal(reasonCodeId, activeSpan.ClosedByCodeId);
        Assert.Equal("F200", activeSpan.ClosedReference);
        Assert.Equal("duty", activeSpan.UpdatedBy);
        Assert.Equal(now.AddMinutes(2), activeSpan.UpdatedAtUtc);

        // half-open semantics
        Assert.True(activeSpan.IsActiveOn(new DateOnly(2026, 02, 10)));
        Assert.False(activeSpan.IsActiveOn(new DateOnly(2026, 02, 11)));
    }

    /// <summary>
    /// CancelTask має переводити span у Canceled (компенсація без видалення) та робити його неактивним.
    /// </summary>
    [Fact]
    public void CancelTask_SetsCanceledStatus_AndDeactivatesSpan()
    {
        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var ep = NewEpisode(openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var documentId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        ep.UpsertTask(
            documentId: documentId,
            missionId: missionId,
            from: new DateOnly(2026, 02, 10),
            toExclusive: null,
            rnokpp: "123",
            fullName: "C",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            author: "tester",
            nowUtc: now);

        var reasonCodeId = Guid.NewGuid();

        ep.CancelTask(
            documentId: documentId,
            missionId: missionId,
            reasonCodeId: reasonCodeId,
            reference: "  VOID  ",
            author: "auditor",
            nowUtc: now.AddMinutes(5));

        var span = ep.TaskSpans.Single();

        Assert.Equal(DocumentStatus.Canceled, span.Status);
        Assert.Equal(reasonCodeId, span.ClosedByCodeId);
        Assert.Equal("VOID", span.ClosedReference);

        // canceled => not active on any date
        Assert.False(span.IsActiveOn(new DateOnly(2026, 02, 10)));
        Assert.False(span.IsActiveOn(new DateOnly(2026, 02, 11)));
    }

    /// <summary>
    /// Якщо span мав причину закриття — повторний UpsertTask має очистити поля Closed*,
    /// бо факт актуалізується документом.
    /// </summary>
    [Fact]
    public void UpsertTask_ClearsClosedFields_AfterReasonClose()
    {
        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var ep = NewEpisode(openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var doc = Guid.NewGuid();
        var mission = Guid.NewGuid();

        ep.UpsertTask(
            documentId: doc,
            missionId: mission,
            from: new DateOnly(2026, 02, 10),
            toExclusive: null,
            rnokpp: "1",
            fullName: "P1",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            author: "tester",
            nowUtc: now);

        ep.CloseTaskByReason(
            closeAtExclusive: new DateOnly(2026, 02, 11),
            reasonCodeId: Guid.NewGuid(),
            reference: "F100",
            author: "duty",
            nowUtc: now.AddMinutes(1));

        var span = ep.TaskSpans.Single();
        Assert.NotNull(span.ClosedByCodeId);
        Assert.NotNull(span.ClosedReference);

        // Upsert again should clear closed fields
        ep.UpsertTask(
            documentId: doc,
            missionId: mission,
            from: new DateOnly(2026, 02, 10),
            toExclusive: null,
            rnokpp: "1",
            fullName: "P1",
            rank: null,
            position: null,
            weapon: null,
            callsign: null,
            author: "tester2",
            nowUtc: now.AddMinutes(2));

        span = ep.TaskSpans.Single();

        Assert.Null(span.ClosedByCombatTaskDocumentId);
        Assert.Null(span.ClosedByCodeId);
        Assert.Null(span.ClosedReference);
    }

    //======================================================================
    // EF persistence tests (SqliteTestDb)
    //======================================================================

    /// <summary>
    /// Перевіряє, що агрегат з TaskSpans коректно зберігається/завантажується через EF (SQLite InMemory).
    /// Це фіксує відповідність домену та EF-конфігів після міграцій.
    /// </summary>
    [Fact]
    public async Task Ef_Roundtrip_PersistsTaskSpanSnapshotAndAudit()
    {
        await using var testDb = new SqliteTestDb();

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var ep = NewEpisode(openedAt: new DateOnly(2026, 02, 01), nowUtc: now);

        var doc = Guid.NewGuid();
        var mission = Guid.NewGuid();

        ep.UpsertTask(
            documentId: doc,
            missionId: mission,
            from: new DateOnly(2026, 02, 05),
            toExclusive: null,
            rnokpp: "1234567890",
            fullName: "Persisted Person",
            rank: "SGT",
            position: "Operator",
            weapon: "Rifle",
            callsign: "FOX",
            author: "tester",
            nowUtc: now);

        // Save
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.TimeSheets.Add(ep);
            await db.SaveChangesAsync();
        }

        // Load
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var loaded = await db.TimeSheets
                .Include(x => x.TaskSpans)
                .SingleAsync(x => x.Id == ep.Id);

            Assert.Single(loaded.TaskSpans);

            var span = loaded.TaskSpans[0];

            Assert.Equal("1234567890", span.Rnokpp);
            Assert.Equal("Persisted Person", span.FullName);
            Assert.Equal("SGT", span.Rank);
            Assert.Equal("Operator", span.Position);
            Assert.Equal("Rifle", span.Weapon);
            Assert.Equal("FOX", span.Callsign);

            Assert.Equal("tester", span.CreatedBy);
            Assert.Equal(now, span.CreatedAtUtc);
            Assert.Equal("tester", span.UpdatedBy);
            Assert.Equal(now, span.UpdatedAtUtc);

            // half-open semantics survives roundtrip
            Assert.True(span.IsActiveOn(new DateOnly(2026, 02, 05)));
            Assert.True(span.IsActiveOn(new DateOnly(2026, 02, 10)));
        }
    }

    //======================================================================
    // Test helpers
    //======================================================================

    /// <summary>
    /// Створює мінімально валідний епізод для тестів.
    /// </summary>
    private static TimeSheetAggregate NewEpisode(DateOnly openedAt, DateTime nowUtc)
        => new()
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            OpenedAt = openedAt,
            ClosedAt = null,
            CreatedBy = "seed",
            CreatedAtUtc = nowUtc,
            ClosedBy = null,
            ClosedAtUtc = null
        };
}
