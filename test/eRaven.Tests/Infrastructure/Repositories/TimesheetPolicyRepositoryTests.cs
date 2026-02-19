//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheets;
using eRaven.Domain.Entities;
using eRaven.Infrastructure;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="TimesheetPolicyRepository"/>.
///
/// <para>
/// Фіксуємо інфраструктурну поведінку довідника кодів і правил переходів:
/// <list type="bullet">
/// <item><description><see cref="TimesheetPolicyRepository.GetCodesAsync"/> не повертає системний код "НБ" та (за замовчуванням) не повертає неактивні коди;</description></item>
/// <item><description><see cref="TimesheetPolicyRepository.GetAllowedTransitionsAsync"/> повертає тільки переходи до активних ToCode і не в "НБ";</description></item>
/// <item><description><see cref="TimesheetPolicyRepository.SavePolicyAsync"/> робить diff-оновлення переходів без втрати Created* у transition;</description></item>
/// <item><description><see cref="TimesheetPolicyRepository.AddCodeAsync"/> / <see cref="TimesheetPolicyRepository.CloseCodeAsync"/> — trim, перевірки, ідемпотентність.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetPolicyRepositoryTests
{
    //======================================================================
    // GetCodesAsync
    //======================================================================

    /// <summary>
    /// GetCodesAsync:
    /// <list type="bullet">
    /// <item><description>за замовчуванням повертає лише активні;</description></item>
    /// <item><description>ніколи не повертає системний код "НБ";</description></item>
    /// <item><description>сортування: SortOrder → Priority → Code.</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task GetCodesAsync_ExcludesSystemNb_AndInactive_AndSorts()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        // Active codes
        var idT = await repo.AddCodeAsync("Т", "Base", null, sortOrder: 0, priority: 10, isTerminal: false, author: "seed", nowUtc: now);
        var id100 = await repo.AddCodeAsync("100", "Task", null, sortOrder: 1, priority: 0, isTerminal: false, author: "seed", nowUtc: now);
        var id30 = await repo.AddCodeAsync("30", "Ready", null, sortOrder: 1, priority: 5, isTerminal: false, author: "seed", nowUtc: now);

        // Inactive code
        var idX = await repo.AddCodeAsync("X", "Closed", null, sortOrder: 2, priority: 0, isTerminal: false, author: "seed", nowUtc: now);
        await repo.CloseCodeAsync(idX, author: "seed", nowUtc: now.AddMinutes(1));

        // System "НБ" inserted directly (repo may allow it, but GetCodes must hide it always)
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            db.TimesheetCodes.Add(new TimesheetCodeDefinition
            {
                Id = Guid.NewGuid(),
                Code = TimesheetSystemCodes.NotInTimesheet,
                Title = "System",
                SortOrder = 0,
                Priority = 0,
                IsTerminal = false,
                IsActive = true,
                CreatedBy = "seed",
                CreatedAtUtc = now
            });
            await db.SaveChangesAsync();
        }

        // 1) Default: active only, no "НБ"
        var activeOnly = await repo.GetCodesAsync(includeInactive: false);
        Assert.Equal(new[] { "Т", "100", "30" }, activeOnly.Select(x => x.Code).ToArray());

        // 2) includeInactive: adds "X" but still no "НБ"
        var withInactive = await repo.GetCodesAsync(includeInactive: true);
        Assert.Equal(new[] { "Т", "100", "30", "X" }, withInactive.Select(x => x.Code).ToArray());

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

    /// <summary>
    /// AddCodeAsync trims поля, робить Description null якщо порожня,
    /// і відхиляє дублікати Code.
    /// </summary>
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
            author: "tester",
            nowUtc: now);

        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var e = await db.TimesheetCodes.SingleAsync(x => x.Id == id);

            Assert.Equal("30", e.Code);
            Assert.Equal("Ready", e.Title);
            Assert.Null(e.Description);

            Assert.True(e.IsActive);
            Assert.Equal("tester", e.CreatedBy);
            Assert.Equal(now, e.CreatedAtUtc);
            Assert.Null(e.UpdatedBy);
            Assert.Null(e.UpdatedAtUtc);
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.AddCodeAsync(
                code: "30",
                title: "Duplicate",
                description: null,
                sortOrder: 0,
                priority: 0,
                isTerminal: false,
                author: "tester",
                nowUtc: now));

        Assert.Contains("вже існує", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// CloseCodeAsync робить код неактивним та заповнює Updated*.
    /// Повторний Close — ідемпотентний.
    /// </summary>
    [Fact]
    public async Task CloseCodeAsync_SetsInactive_AndIsIdempotent()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var t1 = t0.AddMinutes(1);
        var t2 = t0.AddMinutes(2);

        var id = await repo.AddCodeAsync("A", "Alpha", null, 0, 0, false, "seed", t0);

        await repo.CloseCodeAsync(id, "closer1", t1);
        await repo.CloseCodeAsync(id, "closer2", t2); // must not overwrite

        await using var db = await testDb.Factory.CreateDbContextAsync();
        var code = await db.TimesheetCodes.SingleAsync(x => x.Id == id);

        Assert.False(code.IsActive);
        Assert.Equal("closer1", code.UpdatedBy);
        Assert.Equal(t1, code.UpdatedAtUtc);
    }

    /// <summary>
    /// GetCodeByIdAsync повертає тільки активні коди та кидає ArgumentException при порожньому Guid.
    /// </summary>
    [Fact]
    public async Task GetCodeByIdAsync_ReturnsActiveOnly_AndThrowsOnEmptyId()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        var id = await repo.AddCodeAsync("B", "Bravo", null, 0, 0, false, "seed", now);
        Assert.NotNull(await repo.GetCodeByIdAsync(id));

        await repo.CloseCodeAsync(id, "seed", now.AddMinutes(1));
        Assert.Null(await repo.GetCodeByIdAsync(id));

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetCodeByIdAsync(Guid.Empty));
    }

    //======================================================================
    // GetAllowedTransitionsAsync
    //======================================================================

    /// <summary>
    /// GetAllowedTransitionsAsync:
    /// <list type="bullet">
    /// <item><description>повертає переходи тільки в активні ToCode;</description></item>
    /// <item><description>виключає ToCode == "НБ";</description></item>
    /// <item><description>сортування за ToCode.SortOrder → ToCode.Priority → ToCode.Code;</description></item>
    /// <item><description>ToCode має бути завантажений (Include).</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task GetAllowedTransitionsAsync_FiltersAndSorts_AndIncludesToCode()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var now = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);

        // from
        var fromId = await repo.AddCodeAsync("FROM", "From", null, 0, 0, false, "seed", now);

        // to codes (active)
        var toB = await repo.AddCodeAsync("B", "B", null, sortOrder: 0, priority: 5, isTerminal: false, "seed", now);
        var toA = await repo.AddCodeAsync("A", "A", null, sortOrder: 0, priority: 1, isTerminal: false, "seed", now);
        var toC = await repo.AddCodeAsync("C", "C", null, sortOrder: 1, priority: 0, isTerminal: false, "seed", now);

        // inactive ToCode
        var toX = await repo.AddCodeAsync("X", "X", null, 2, 0, false, "seed", now);
        await repo.CloseCodeAsync(toX, "seed", now.AddMinutes(1));

        // system ToCode "НБ" (active)
        Guid nbId;
        await using (var db = await testDb.Factory.CreateDbContextAsync())
        {
            var nb = new TimesheetCodeDefinition
            {
                Id = Guid.NewGuid(),
                Code = TimesheetSystemCodes.NotInTimesheet,
                Title = "System",
                SortOrder = 0,
                Priority = 0,
                IsTerminal = false,
                IsActive = true,
                CreatedBy = "seed",
                CreatedAtUtc = now
            };
            db.TimesheetCodes.Add(nb);
            await db.SaveChangesAsync();
            nbId = nb.Id;

            // Seed transitions
            db.TimesheetCodeTransitions.AddRange(
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = toB, StartShiftDays = 0, CreatedBy = "seed", CreatedAtUtc = now },
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = toA, StartShiftDays = 1, CreatedBy = "seed", CreatedAtUtc = now },
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = toC, StartShiftDays = 0, CreatedBy = "seed", CreatedAtUtc = now },
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = toX, StartShiftDays = 0, CreatedBy = "seed", CreatedAtUtc = now },   // inactive -> must be filtered
                new TimesheetCodeTransition { Id = Guid.NewGuid(), FromCodeId = fromId, ToCodeId = nbId, StartShiftDays = 0, CreatedBy = "seed", CreatedAtUtc = now }    // "НБ" -> must be filtered
            );

            await db.SaveChangesAsync();
        }

        var list = await repo.GetAllowedTransitionsAsync(fromId);

        // Expected only active non-system: A, B, C sorted by (sortOrder, priority, code)
        Assert.Equal(new[] { "A", "B", "C" }, list.Select(x => x.ToCode.Code).ToArray());

        // Include(ToCode) must work
        Assert.All(list, t => Assert.NotNull(t.ToCode));
    }

    //======================================================================
    // SavePolicyAsync
    //======================================================================

    /// <summary>
    /// SavePolicyAsync має:
    /// <list type="bullet">
    /// <item><description>оновити Title/Description/SortOrder/Priority/IsTerminal + Updated*;</description></item>
    /// <item><description>diff-оновити transitions: update StartShiftDays, add нові, remove відсутні;</description></item>
    /// <item><description>не змінювати CreatedBy/CreatedAtUtc у вже існуючих переходів.</description></item>
    /// </list>
    /// </summary>
    [Fact]
    public async Task SavePolicyAsync_UpdatesCode_AndDiffUpdatesTransitions_PreservingTransitionCreated()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var t1 = t0.AddMinutes(5);

        // from code
        var fromId = await repo.AddCodeAsync("FROM", "OldTitle", "OldDesc", 10, 10, false, "seed", t0);

        // to codes
        var to1 = await repo.AddCodeAsync("T1", "T1", null, 0, 0, false, "seed", t0);
        var to2 = await repo.AddCodeAsync("T2", "T2", null, 1, 0, false, "seed", t0);
        var to3 = await repo.AddCodeAsync("T3", "T3", null, 2, 0, false, "seed", t0);

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
        // - update T1 to StartShiftDays=1 (duplicate entry later ignored by normalization)
        // - remove T2 (not present)
        // - add T3 (0)
        var desired = new[]
        {
            new TimesheetTransitionSpecDto(to1, 1),
            new TimesheetTransitionSpecDto(to1, 0),          // duplicate; should be ignored
            new TimesheetTransitionSpecDto(to3, 0),
            new TimesheetTransitionSpecDto(Guid.Empty, 0),   // ignored
            new TimesheetTransitionSpecDto(fromId, 0)        // self; ignored
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
            Assert.Equal("FROM", code.Code);                // immutable
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

    /// <summary>
    /// SavePolicyAsync має відхиляти transitions на неіснуючі або неактивні ToCode,
    /// і не робити часткових змін.
    /// </summary>
    [Fact]
    public async Task SavePolicyAsync_Throws_WhenToCodeMissingOrInactive_WithoutPartialChanges()
    {
        await using var testDb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(testDb.Factory);

        var t0 = new DateTime(2026, 02, 17, 10, 00, 00, DateTimeKind.Utc);
        var t1 = t0.AddMinutes(5);

        var fromId = await repo.AddCodeAsync("FROM", "Title", null, 0, 0, false, "seed", t0);
        var toActive = await repo.AddCodeAsync("OK", "OK", null, 0, 0, false, "seed", t0);

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
                allowedTransitions: new[] { new TimesheetTransitionSpecDto(missingTo, 0) },
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
        var toInactive = await repo.AddCodeAsync("Z", "Z", null, 1, 0, false, "seed", t0);
        await repo.CloseCodeAsync(toInactive, "seed", t0.AddMinutes(1));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.SavePolicyAsync(
                codeId: fromId,
                title: "New2",
                description: null,
                sortOrder: 0,
                priority: 0,
                isTerminal: false,
                allowedTransitions: new[] { new TimesheetTransitionSpecDto(toInactive, 0) },
                author: "editor",
                nowUtc: t1));
    }
}
