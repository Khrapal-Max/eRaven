//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyRepositoryTests
//-----------------------------------------------------------------------------
//
// Покриваємо:
// - довідник кодів (GetCodesAsync/GetCodeByIdAsync/Add/Close)
// - матрицю переходів (GetAllowedCodesAsync/GetTransitionCodesAsync/GetEmergencyCodesAsync/SavePolicyAsync)
// - глобальні emergency коди (GetEmergencyCodesAsync)
//
// Інваріанти:
// - "НБ" є derived (не реальний код) => репозиторій завжди його приховує, навіть якщо legacy-рядок є в БД.
// - SystemCode не створюється/не закривається/не має policy.
// - strict matrix: тільки TransitionCode → TransitionCode.
//-----------------------------------------------------------------------------

using eRaven.Domain.Consts;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.ValueObjects;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetPolicyRepositoryTests
{
    //======================================================================
    // GetCodesAsync
    //======================================================================

    [Fact]
    public async Task GetCodesAsync_ExcludesDerivedNb_AndInactive_AndSorts()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        // Active transition codes
        var idT = await repo.AddCodeAsync(
            code: "Т",
            title: "Base",
            description: null,
            sortOrder: 0,
            priority: 10,
            isTerminal: false,
            roleCode: RoleCode.TransitionCode,
            uiStyle: TimesheetUiStyle.Warning,
            author: "seed",
            nowUtc: now);

        _ = await repo.AddCodeAsync(
            code: "30",
            title: "Ready",
            description: null,
            sortOrder: 1,
            priority: 5,
            isTerminal: false,
            roleCode: RoleCode.TransitionCode,
            uiStyle: TimesheetUiStyle.Ready,
            author: "seed",
            nowUtc: now);

        // Inactive code
        var idX = await repo.AddCodeAsync(
            code: "X",
            title: "Closed",
            description: null,
            sortOrder: 2,
            priority: 0,
            isTerminal: false,
            roleCode: RoleCode.TransitionCode,
            uiStyle: TimesheetUiStyle.Warning,
            author: "seed",
            nowUtc: now);

        await repo.CloseCodeAsync(idX, author: "seed", nowUtc: now.AddMinutes(1));

        // Insert system code "100" directly (seed-style).
        // Insert derived "НБ" directly (legacy row): repo MUST hide it.
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.TimesheetCodes.Add(new TimesheetCodeDefinition
            {
                Id = Guid.NewGuid(),
                Code = "100",
                Title = "Task",
                Description = null,
                SortOrder = 1,
                Priority = 0,
                IsTerminal = false,
                IsActive = true,
                RoleCode = RoleCode.SystemCode,
                UiStyle = TimesheetUiStyle.Danger,
                CreatedBy = "seed",
                CreatedAtUtc = now
            });

            db.TimesheetCodes.Add(new TimesheetCodeDefinition
            {
                Id = Guid.NewGuid(),
                Code = TimesheetDerivedCodes.NotInTimesheet,
                Title = "Derived",
                Description = null,
                SortOrder = 0,
                Priority = 0,
                IsTerminal = false,
                IsActive = true,
                RoleCode = RoleCode.TransitionCode,
                UiStyle = TimesheetUiStyle.NotInTimesheet,
                CreatedBy = "seed",
                CreatedAtUtc = now
            });

            await db.SaveChangesAsync();
        }

        // 1) Default: active only, no "НБ"
        var activeOnly = await repo.GetCodesAsync(includeInactive: false);
        Assert.Equal(["Т", "100", "30"], [.. activeOnly.Select(x => x.Code)]);

        // 2) includeInactive: adds "X" but still no "НБ"
        var withInactive = await repo.GetCodesAsync(includeInactive: true);
        Assert.Equal(["Т", "100", "30", "X"], [.. withInactive.Select(x => x.Code)]);

        // Sanity: GetCodeByIdAsync returns active only
        var t = await repo.GetCodeByIdAsync(idT);
        Assert.NotNull(t);
        Assert.Equal("Т", t!.Code);

        var closed = await repo.GetCodeByIdAsync(idX);
        Assert.Null(closed);
    }

    //======================================================================
    // AddCodeAsync / CloseCodeAsync / GetCodeByIdAsync
    //======================================================================

    [Fact]
    public async Task AddCodeAsync_Trims_AndThrowsOnDuplicate()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var id = await repo.AddCodeAsync(
            code: "  30  ",
            title: "  Ready  ",
            description: "   ",
            sortOrder: 1,
            priority: 2,
            isTerminal: false,
            roleCode: RoleCode.TransitionCode,
            uiStyle: TimesheetUiStyle.Ready,
            author: "tester",
            nowUtc: now);

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var e = await db.TimesheetCodes.SingleAsync(x => x.Id == id);

            Assert.Equal("30", e.Code);
            Assert.Equal("Ready", e.Title);
            Assert.Null(e.Description);

            Assert.Equal(RoleCode.TransitionCode, e.RoleCode);
            Assert.Equal(TimesheetUiStyle.Ready, e.UiStyle);
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.AddCodeAsync(
                code: "30",
                title: "Duplicate",
                description: null,
                sortOrder: 0,
                priority: 0,
                isTerminal: false,
                roleCode: RoleCode.TransitionCode,
                uiStyle: TimesheetUiStyle.Warning,
                author: "tester",
                nowUtc: now));

        Assert.Contains("вже існує", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddCodeAsync_Throws_ForSystemCode_AndDerivedNb()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.AddCodeAsync(
                code: "100",
                title: "Task",
                description: null,
                sortOrder: 0,
                priority: 0,
                isTerminal: false,
                roleCode: RoleCode.SystemCode,
                uiStyle: TimesheetUiStyle.Danger,
                author: "seed",
                nowUtc: now));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.AddCodeAsync(
                code: TimesheetDerivedCodes.NotInTimesheet,
                title: "NB",
                description: null,
                sortOrder: 0,
                priority: 0,
                isTerminal: false,
                roleCode: RoleCode.TransitionCode,
                uiStyle: TimesheetUiStyle.NotInTimesheet,
                author: "seed",
                nowUtc: now));
    }

    [Fact]
    public async Task CloseCodeAsync_SetsInactive_AndIsIdempotent()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var t1 = t0.AddMinutes(1);
        var t2 = t0.AddMinutes(2);

        var id = await repo.AddCodeAsync(
            code: "A",
            title: "Alpha",
            description: null,
            sortOrder: 0,
            priority: 0,
            isTerminal: false,
            roleCode: RoleCode.TransitionCode,
            uiStyle: TimesheetUiStyle.Warning,
            author: "seed",
            nowUtc: t0);

        await repo.CloseCodeAsync(id, author: "closer1", nowUtc: t1);
        await repo.CloseCodeAsync(id, author: "closer2", nowUtc: t2); // must not overwrite

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var code = await db.TimesheetCodes.SingleAsync(x => x.Id == id);

        Assert.False(code.IsActive);
        Assert.Equal("closer1", code.UpdatedBy);
        Assert.Equal(t1, code.UpdatedAtUtc);
    }

    [Fact]
    public async Task GetCodeByIdAsync_ReturnsActiveOnly_AndThrowsOnEmptyId()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var id = await repo.AddCodeAsync(
            code: "B",
            title: "Bravo",
            description: null,
            sortOrder: 0,
            priority: 0,
            isTerminal: false,
            roleCode: RoleCode.TransitionCode,
            uiStyle: TimesheetUiStyle.Warning,
            author: "seed",
            nowUtc: now);

        Assert.NotNull(await repo.GetCodeByIdAsync(id));

        await repo.CloseCodeAsync(id, author: "seed", nowUtc: now.AddMinutes(1));
        Assert.Null(await repo.GetCodeByIdAsync(id));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetCodeByIdAsync(Guid.Empty));
    }


    //======================================================================
    // GetEmergencyCodesAsync
    //======================================================================

    [Fact]
    public async Task GetEmergencyCodesAsync_ReturnsOnlyActiveEmergency_AndSorts()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        // Transition code should not be returned.
        _ = await repo.AddCodeAsync("T", "T", null, 0, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", now);

        // Emergency codes (active)
        _ = await repo.AddCodeAsync("200", "E2", null, 50, 0, false, RoleCode.EmergencyCode, TimesheetUiStyle.Danger, "seed", now);
        _ = await repo.AddCodeAsync("Ф100", "Fact", null, 70, 0, false, RoleCode.EmergencyCode, TimesheetUiStyle.Danger, "seed", now);

        // Emergency (inactive) must be excluded
        var ex = await repo.AddCodeAsync("201", "EX", null, 60, 0, false, RoleCode.EmergencyCode, TimesheetUiStyle.Danger, "seed", now);
        await repo.CloseCodeAsync(ex, "seed", now.AddMinutes(1));

        var list = await repo.GetEmergencyCodesAsync();

        Assert.Equal(["200", "Ф100"], [.. list.Select(x => x.Code)]);
        Assert.All(list, x => Assert.True(x.IsActive));
        Assert.All(list, x => Assert.Equal(RoleCode.EmergencyCode, x.RoleCode));
    }

    //======================================================================
    // GetTransitionCodesAsync
    //======================================================================

    [Fact]
    public async Task GetTransitionCodesAsync_ReturnsTransitionTargets_IncludingInactive_AndFiltersNonTransition()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var fromId = await repo.AddCodeAsync("FROM", "From", null, 0, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", now);

        // Transition targets
        var toA = await repo.AddCodeAsync("A", "A", null, 0, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", now);
        var toX = await repo.AddCodeAsync("X", "X", null, 1, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", now);
        await repo.CloseCodeAsync(toX, "seed", now.AddMinutes(1)); // inactive target should still be returned by policy

        // Emergency target should be filtered out
        var em = await repo.AddCodeAsync("200", "E", null, 50, 0, false, RoleCode.EmergencyCode, TimesheetUiStyle.Danger, "seed", now);

        // Legacy NB target should be filtered out
        Guid nbId;
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var nb = new TimesheetCodeDefinition
            {
                Id = Guid.NewGuid(),
                Code = TimesheetDerivedCodes.NotInTimesheet,
                Title = "Derived",
                Description = null,
                SortOrder = 0,
                Priority = 0,
                IsTerminal = false,
                IsActive = true,
                RoleCode = RoleCode.TransitionCode,
                UiStyle = TimesheetUiStyle.NotInTimesheet,
                CreatedBy = "seed",
                CreatedAtUtc = now
            };
            db.TimesheetCodes.Add(nb);
            nbId = nb.Id;

            db.TimesheetCodeTransitions.AddRange(
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = toA, StartShiftDays = 0, CreatedBy = "seed", CreatedAtUtc = now },
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = toX, StartShiftDays = 1, CreatedBy = "seed", CreatedAtUtc = now },
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = em, StartShiftDays = 0, CreatedBy = "seed", CreatedAtUtc = now },
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = nbId, StartShiftDays = 0, CreatedBy = "seed", CreatedAtUtc = now }
            );

            await db.SaveChangesAsync();
        }

        var policy = await repo.GetTransitionCodesAsync(fromId);

        // Only TransitionCode targets (A + X), and X is inactive but still returned
        Assert.Equal(["A", "X"], [.. policy.Select(x => x.ToCode.Code)]);
        Assert.All(policy, t => Assert.NotNull(t.ToCode));
        Assert.All(policy, t => Assert.Equal(RoleCode.TransitionCode, t.ToCode.RoleCode));
    }

    //======================================================================
    // GetAllowedCodesAsync
    //======================================================================

    [Fact]
    public async Task GetAllowedCodesAsync_ReturnsTransitionTargetsPlusEmergency_ActiveOnly()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var fromId = await repo.AddCodeAsync("FROM", "From", null, 0, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", now);

        // Transition targets (matrix)
        var toA = await repo.AddCodeAsync("A", "A", null, 0, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", now);
        var toB = await repo.AddCodeAsync("B", "B", null, 1, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", now);

        // Emergency codes (global options)
        _ = await repo.AddCodeAsync("200", "E", null, 50, 0, false, RoleCode.EmergencyCode, TimesheetUiStyle.Danger, "seed", now);

        var f100 = await repo.AddCodeAsync("Ф100", "Fact", null, 70, 0, false, RoleCode.EmergencyCode, TimesheetUiStyle.Danger, "seed", now);

        // inactive emergency should be excluded
        var emX = await repo.AddCodeAsync("201", "EX", null, 60, 0, false, RoleCode.EmergencyCode, TimesheetUiStyle.Danger, "seed", now);
        await repo.CloseCodeAsync(emX, "seed", now.AddMinutes(1));

        // seed matrix transitions only for A,B
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.TimesheetCodeTransitions.AddRange(
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = toA, StartShiftDays = 0, CreatedBy = "seed", CreatedAtUtc = now },
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = toB, StartShiftDays = 1, CreatedBy = "seed", CreatedAtUtc = now }
            );
            await db.SaveChangesAsync();
        }

        var allowed = await repo.GetAllowedCodesAsync(fromId);

        // Contains transition targets + emergency codes (active only).
        Assert.Equal(["A", "B", "200", "Ф100"], [.. allowed.Select(x => x.ToCode.Code)]);

        // Sanity: all returned targets are active and not derived NB
        Assert.All(allowed, x => Assert.True(x.ToCode.IsActive));
        Assert.DoesNotContain(allowed, x => x.ToCode.Code == TimesheetDerivedCodes.NotInTimesheet);

        // Sanity: emergency codes returned as synthetic transitions (Id may be empty)
        Assert.Contains(allowed, x => x.ToCode.Code == "Ф100");
        Assert.DoesNotContain(allowed, x => x.ToCodeId == emX);
        Assert.NotNull(await repo.GetCodeByIdAsync(f100)); // emergency is still a real code
    }

    //======================================================================
    // SavePolicyAsync
    //======================================================================

    [Fact]
    public async Task SavePolicyAsync_UpdatesCode_AndDiffUpdatesTransitions_PreservingTransitionCreated()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var t1 = t0.AddMinutes(5);

        // from code
        var fromId = await repo.AddCodeAsync(
            code: "FROM",
            title: "OldTitle",
            description: "OldDesc",
            sortOrder: 10,
            priority: 10,
            isTerminal: false,
            roleCode: RoleCode.TransitionCode,
            uiStyle: TimesheetUiStyle.Warning,
            author: "seed",
            nowUtc: t0);

        // to codes
        var to1 = await repo.AddCodeAsync("T1", "T1", null, 0, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", t0);
        var to2 = await repo.AddCodeAsync("T2", "T2", null, 1, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", t0);
        var to3 = await repo.AddCodeAsync("T3", "T3", null, 2, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", t0);

        // seed existing transitions FROM -> T1 (0), FROM -> T2 (0)
        Guid trTo1Id;
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var tr1 = new TimesheetCodeTransition
            {
                Id = Guid.NewGuid(),
                FromCodeId = fromId,
                ToCodeId = to1,
                StartShiftDays = 0,
                CreatedBy = "seed",
                CreatedAtUtc = t0
            };
            var tr2 = new TimesheetCodeTransition
            {
                Id = Guid.NewGuid(),
                FromCodeId = fromId,
                ToCodeId = to2,
                StartShiftDays = 0,
                CreatedBy = "seed",
                CreatedAtUtc = t0
            };

            db.TimesheetCodeTransitions.AddRange(tr1, tr2);
            await db.SaveChangesAsync();
            trTo1Id = tr1.Id;
        }

        // desired transitions:
        // - update T1 to StartShiftDays=1
        // - remove T2 (not present)
        // - add T3 (0)
        var desired = new[]
        {
            new TimesheetTransitionSpec(to1, 1),
            new TimesheetTransitionSpec(to1, 0),        // duplicate; ignored by normalization
            new TimesheetTransitionSpec(to3, 0),
            new TimesheetTransitionSpec(Guid.Empty, 0), // ignored
            new TimesheetTransitionSpec(fromId, 0)      // self; ignored
        };

        await repo.SavePolicyAsync(
            codeId: fromId,
            title: "  NewTitle  ",
            description: "  NewDesc  ",
            sortOrder: 1,
            priority: 2,
            isTerminal: true,
            allowedTransitions: desired,
            author: "editor",
            nowUtc: t1);

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var code = await db.TimesheetCodes.SingleAsync(x => x.Id == fromId);
            Assert.Equal("FROM", code.Code); // immutable
            Assert.Equal("NewTitle", code.Title);
            Assert.Equal("NewDesc", code.Description);
            Assert.Equal(1, code.SortOrder);
            Assert.Equal(2, code.Priority);
            Assert.True(code.IsTerminal);
            Assert.Equal("editor", code.UpdatedBy);
            Assert.Equal(t1, code.UpdatedAtUtc);

            var transitions = await db.TimesheetCodeTransitions
                .Where(x => x.FromCodeId == fromId)
                .OrderBy(x => x.ToCodeId)
                .ToListAsync();

            // T2 removed, T1 updated, T3 added => 2 total
            Assert.Equal(2, transitions.Count);

            var tr1 = transitions.Single(x => x.ToCodeId == to1);
            Assert.Equal(1, tr1.StartShiftDays);
            // Created* preserved for existing transition
            Assert.Equal(trTo1Id, tr1.Id);
            Assert.Equal("seed", tr1.CreatedBy);
            Assert.Equal(t0, tr1.CreatedAtUtc);

            var tr3 = transitions.Single(x => x.ToCodeId == to3);
            Assert.Equal(0, tr3.StartShiftDays);
            Assert.Equal("editor", tr3.CreatedBy);
            Assert.Equal(t1, tr3.CreatedAtUtc);
        }
    }

    [Fact]
    public async Task SavePolicyAsync_Throws_WhenToCodeMissingOrInactive_WithoutPartialChanges()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var t1 = t0.AddMinutes(5);

        var fromId = await repo.AddCodeAsync("FROM", "Title", null, 0, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", t0);
        var toActive = await repo.AddCodeAsync("OK", "OK", null, 0, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", t0);

        // Seed one existing transition
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.TimesheetCodeTransitions.Add(new TimesheetCodeTransition
            {
                Id = Guid.NewGuid(),
                FromCodeId = fromId,
                ToCodeId = toActive,
                StartShiftDays = 0,
                CreatedBy = "seed",
                CreatedAtUtc = t0
            });
            await db.SaveChangesAsync();
        }

        // Snapshot before
        string beforeTitle;
        int beforeTransitions;
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            beforeTitle = (await db.TimesheetCodes.SingleAsync(x => x.Id == fromId)).Title;
            beforeTransitions = await db.TimesheetCodeTransitions.CountAsync(x => x.FromCodeId == fromId);
        }

        // Case 1: missing ToCode
        var missingTo = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.SavePolicyAsync(
                codeId: fromId,
                title: "New",
                description: null,
                sortOrder: 0,
                priority: 0,
                isTerminal: false,
                allowedTransitions: [new TimesheetTransitionSpec(missingTo, 0)],
                author: "editor",
                nowUtc: t1));

        // Verify no partial changes
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var code = await db.TimesheetCodes.SingleAsync(x => x.Id == fromId);
            Assert.Equal(beforeTitle, code.Title);

            var count = await db.TimesheetCodeTransitions.CountAsync(x => x.FromCodeId == fromId);
            Assert.Equal(beforeTransitions, count);
        }

        // Case 2: inactive ToCode
        var toInactive = await repo.AddCodeAsync("Z", "Z", null, 1, 0, false, RoleCode.TransitionCode, TimesheetUiStyle.Warning, "seed", t0);
        await repo.CloseCodeAsync(toInactive, "seed", t0.AddMinutes(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.SavePolicyAsync(
                codeId: fromId,
                title: "New2",
                description: null,
                sortOrder: 0,
                priority: 0,
                isTerminal: false,
                allowedTransitions: [new TimesheetTransitionSpec(toInactive, 0)],
                author: "editor",
                nowUtc: t1));
    }
}
