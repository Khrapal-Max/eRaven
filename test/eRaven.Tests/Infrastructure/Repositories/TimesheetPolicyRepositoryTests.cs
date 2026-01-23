//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetPolicyRepositoryTests
{
    private static TimesheetCodeDefinition NewCode(
        TimesheetLane lane,
        string code,
        bool isActive = true,
        int sort = 0)
        => new()
        {
            Id = Guid.NewGuid(),
            Lane = lane,
            Code = code,
            Title = code,
            SortOrder = sort,
            IsActive = isActive,
            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

    private static TimesheetCodeTransition NewTransition(TimesheetLane lane, Guid fromId, Guid toId)
        => new()
        {
            Id = Guid.NewGuid(),
            Lane = lane,
            FromCodeId = fromId,
            ToCodeId = toId,
            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

    [Fact]
    public async Task GetCodesAsync_filters_by_lane_and_active_and_orders()
    {
        await using var tdb = new SqliteTestDb();

        using (var db = tdb.Factory.CreateDbContext())
        {
            var m2 = NewCode(TimesheetLane.Main, "B", isActive: true, sort: 1);
            var m1 = NewCode(TimesheetLane.Main, "A", isActive: true, sort: 1);
            var m0 = NewCode(TimesheetLane.Main, "Z", isActive: false, sort: 0); // filtered out
            var t1 = NewCode(TimesheetLane.Task, "T", isActive: true, sort: 0);

            db.TimesheetCodes.AddRange(m2, m1, m0, t1);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetPolicyRepository(tdb.Factory);
        var result = await repo.GetCodesAsync(TimesheetLane.Main);

        Assert.Equal(2, result.Count);
        Assert.Equal("A", result[0].Code); // sort=1 then Code asc
        Assert.Equal("B", result[1].Code);
        Assert.All(result, x => Assert.Equal(TimesheetLane.Main, x.Lane));
        Assert.All(result, x => Assert.True(x.IsActive));
    }

    [Fact]
    public async Task GetAllowedNextAsync_returns_to_ids()
    {
        await using var tdb = new SqliteTestDb();

        Guid fromId;
        Guid to1Id;
        Guid to2Id;

        using (var db = tdb.Factory.CreateDbContext())
        {
            var from = NewCode(TimesheetLane.Main, "30");
            var to1 = NewCode(TimesheetLane.Main, "A");
            var to2 = NewCode(TimesheetLane.Main, "B");

            fromId = from.Id;
            to1Id = to1.Id;
            to2Id = to2.Id;

            db.TimesheetCodes.AddRange(from, to1, to2);
            db.TimesheetCodeTransitions.AddRange(
                NewTransition(TimesheetLane.Main, from.Id, to1.Id),
                NewTransition(TimesheetLane.Main, from.Id, to2.Id)
            );

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetPolicyRepository(tdb.Factory);
        var set = await repo.GetAllowedNextAsync(fromId);

        Assert.Equal(2, set.Count);
        Assert.Contains(to1Id, set);
        Assert.Contains(to2Id, set);
    }

    [Fact]
    public async Task SavePolicyAsync_throws_when_from_not_found()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await repo.SavePolicyAsync(
                lane: TimesheetLane.Main,
                fromCodeId: Guid.NewGuid(),
                endDateMeaning: TimesheetEndDateMeaning.LastDayOfThisCode,
                nextCodeOnEnd: null,
                allowedToCodeIds: [],
                author: "ui",
                nowUtc: DateTime.UtcNow));
    }

    [Fact]
    public async Task SavePolicyAsync_throws_when_lane_mismatch()
    {
        await using var tdb = new SqliteTestDb();

        Guid fromId;
        using (var db = tdb.Factory.CreateDbContext())
        {
            var from = NewCode(TimesheetLane.Main, "30");
            fromId = from.Id;

            db.TimesheetCodes.Add(from);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetPolicyRepository(tdb.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.SavePolicyAsync(
                lane: TimesheetLane.Task, // mismatch
                fromCodeId: fromId,
                endDateMeaning: TimesheetEndDateMeaning.LastDayOfThisCode,
                nextCodeOnEnd: null,
                allowedToCodeIds: [],
                author: "ui",
                nowUtc: DateTime.UtcNow));
    }

    [Fact]
    public async Task SavePolicyAsync_sets_nextCodeOnEnd_default_30_for_Main_FirstDayOfNextCode()
    {
        await using var tdb = new SqliteTestDb();

        Guid fromId;
        Guid toId;

        using (var db = tdb.Factory.CreateDbContext())
        {
            var code30 = NewCode(TimesheetLane.Main, "30");
            var from = NewCode(TimesheetLane.Main, "ВП");
            var to = NewCode(TimesheetLane.Main, "ЛХ");

            fromId = from.Id;
            toId = to.Id;

            db.TimesheetCodes.AddRange(code30, from, to);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetPolicyRepository(tdb.Factory);
        var now = DateTime.UtcNow;

        await repo.SavePolicyAsync(
            lane: TimesheetLane.Main,
            fromCodeId: fromId,
            endDateMeaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
            nextCodeOnEnd: null, // should default to "30"
            allowedToCodeIds: [toId],
            author: "ui",
            nowUtc: now);

        using var db2 = tdb.Factory.CreateDbContext();
        var updated = await db2.TimesheetCodes.SingleAsync(x => x.Id == fromId);

        Assert.Equal(TimesheetEndDateMeaning.FirstDayOfNextCode, updated.EndDateMeaning);
        Assert.Equal("30", updated.NextCodeOnEnd);
        Assert.Equal("ui", updated.UpdatedBy);

        Assert.NotNull(updated.UpdatedAtUtc);
        Assert.True((updated.UpdatedAtUtc.Value - now).Duration() < TimeSpan.FromSeconds(2));

        var tr = await db2.TimesheetCodeTransitions.Where(x => x.FromCodeId == fromId).ToListAsync();
        Assert.Single(tr);
        Assert.Equal(toId, tr[0].ToCodeId);
        Assert.Equal(TimesheetLane.Main, tr[0].Lane);
    }

    [Fact]
    public async Task SavePolicyAsync_throws_when_nextCodeOnEnd_missing_for_Main_FirstDayOfNextCode()
    {
        await using var tdb = new SqliteTestDb();

        Guid fromId;
        using (var db = tdb.Factory.CreateDbContext())
        {
            var from = NewCode(TimesheetLane.Main, "ВП");
            fromId = from.Id;

            db.TimesheetCodes.Add(from);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.SavePolicyAsync(
                lane: TimesheetLane.Main,
                fromCodeId: fromId,
                endDateMeaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
                nextCodeOnEnd: "30", // but 30 does not exist in DB
                allowedToCodeIds: [],
                author: "ui",
                nowUtc: DateTime.UtcNow));

        Assert.Contains("NextCodeOnEnd", ex.Message);
    }

    [Fact]
    public async Task SavePolicyAsync_nulls_nextCodeOnEnd_for_LastDayOfThisCode_and_for_Task()
    {
        await using var tdb = new SqliteTestDb();

        Guid fromMainId;
        Guid fromTaskId;
        Guid toMainId;
        Guid toTaskId;

        using (var db = tdb.Factory.CreateDbContext())
        {
            var fromMain = NewCode(TimesheetLane.Main, "ВДР");
            var fromTask = NewCode(TimesheetLane.Task, "PLAN");
            var toMain = NewCode(TimesheetLane.Main, "30");
            var toTask = NewCode(TimesheetLane.Task, "X");

            fromMainId = fromMain.Id;
            fromTaskId = fromTask.Id;
            toMainId = toMain.Id;
            toTaskId = toTask.Id;

            db.TimesheetCodes.AddRange(fromMain, fromTask, toMain, toTask);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetPolicyRepository(tdb.Factory);
        var now = DateTime.UtcNow;

        await repo.SavePolicyAsync(
            lane: TimesheetLane.Main,
            fromCodeId: fromMainId,
            endDateMeaning: TimesheetEndDateMeaning.LastDayOfThisCode,
            nextCodeOnEnd: "30", // should be forced null
            allowedToCodeIds: [toMainId],
            author: "ui",
            nowUtc: now);

        await repo.SavePolicyAsync(
            lane: TimesheetLane.Task,
            fromCodeId: fromTaskId,
            endDateMeaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
            nextCodeOnEnd: "30", // should be forced null for Task
            allowedToCodeIds: [toTaskId],
            author: "ui",
            nowUtc: now);

        using var db2 = tdb.Factory.CreateDbContext();
        var updMain = await db2.TimesheetCodes.SingleAsync(x => x.Id == fromMainId);
        var updTask = await db2.TimesheetCodes.SingleAsync(x => x.Id == fromTaskId);

        Assert.Null(updMain.NextCodeOnEnd);
        Assert.Null(updTask.NextCodeOnEnd);
    }

    [Fact]
    public async Task SavePolicyAsync_rewrites_transitions_and_clears_when_empty()
    {
        await using var tdb = new SqliteTestDb();

        Guid fromId;
        Guid to1Id;
        Guid to2Id;

        using (var db = tdb.Factory.CreateDbContext())
        {
            var code30 = NewCode(TimesheetLane.Main, "30");
            var from = NewCode(TimesheetLane.Main, "ВП");
            var to1 = NewCode(TimesheetLane.Main, "ЛХ");
            var to2 = NewCode(TimesheetLane.Main, "СЗЧ");

            fromId = from.Id;
            to1Id = to1.Id;
            to2Id = to2.Id;

            db.TimesheetCodes.AddRange(code30, from, to1, to2);

            // existing transition (will be replaced)
            db.TimesheetCodeTransitions.Add(NewTransition(TimesheetLane.Main, from.Id, to1.Id));

            await db.SaveChangesAsync();
        }

        var repo = new TimesheetPolicyRepository(tdb.Factory);

        // rewrite -> only to2 (and ignore self if passed)
        await repo.SavePolicyAsync(
            lane: TimesheetLane.Main,
            fromCodeId: fromId,
            endDateMeaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
            nextCodeOnEnd: "30",
            allowedToCodeIds: [to2Id, fromId], // fromId ignored
            author: "ui",
            nowUtc: DateTime.UtcNow);

        using (var db2 = tdb.Factory.CreateDbContext())
        {
            var after = await db2.TimesheetCodeTransitions.Where(x => x.FromCodeId == fromId).ToListAsync();
            Assert.Single(after);
            Assert.Equal(to2Id, after[0].ToCodeId);
        }

        // clear transitions
        await repo.SavePolicyAsync(
            lane: TimesheetLane.Main,
            fromCodeId: fromId,
            endDateMeaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
            nextCodeOnEnd: "30",
            allowedToCodeIds: [],
            author: "ui",
            nowUtc: DateTime.UtcNow);

        using var db3 = tdb.Factory.CreateDbContext();
        var cleared = await db3.TimesheetCodeTransitions.Where(x => x.FromCodeId == fromId).ToListAsync();
        Assert.Empty(cleared);
    }

    [Fact]
    public async Task SavePolicyAsync_throws_if_allowed_contains_other_lane_or_inactive()
    {
        await using var tdb = new SqliteTestDb();

        Guid fromId;
        Guid otherLaneId;
        Guid inactiveId;

        using (var db = tdb.Factory.CreateDbContext())
        {
            var code30 = NewCode(TimesheetLane.Main, "30");
            var from = NewCode(TimesheetLane.Main, "ВП");
            var toTask = NewCode(TimesheetLane.Task, "PLAN");   // other lane
            var toInactive = NewCode(TimesheetLane.Main, "X", isActive: false);

            fromId = from.Id;
            otherLaneId = toTask.Id;
            inactiveId = toInactive.Id;

            db.TimesheetCodes.AddRange(code30, from, toTask, toInactive);
            await db.SaveChangesAsync();
        }

        var repo = new TimesheetPolicyRepository(tdb.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.SavePolicyAsync(
                lane: TimesheetLane.Main,
                fromCodeId: fromId,
                endDateMeaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
                nextCodeOnEnd: "30",
                allowedToCodeIds: [otherLaneId],
                author: "ui",
                nowUtc: DateTime.UtcNow));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await repo.SavePolicyAsync(
                lane: TimesheetLane.Main,
                fromCodeId: fromId,
                endDateMeaning: TimesheetEndDateMeaning.FirstDayOfNextCode,
                nextCodeOnEnd: "30",
                allowedToCodeIds: [inactiveId],
                author: "ui",
                nowUtc: DateTime.UtcNow));
    }
}
