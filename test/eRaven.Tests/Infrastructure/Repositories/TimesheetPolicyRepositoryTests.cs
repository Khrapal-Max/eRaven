//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class TimesheetPolicyRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 23, 12, 0, 0, DateTimeKind.Utc);

    private static TimesheetCodeDefinition NewCode(
      string code,
      string title,
      int sortOrder,
      int priority,
      bool isActive = true,
      bool isTerminal = false,
      string createdBy = "seed")
      => new()
      {
          Id = Guid.NewGuid(),
          Code = code,
          Title = title,
          Description = null,
          SortOrder = sortOrder,
          Priority = priority,
          IsTerminal = isTerminal,
          IsActive = isActive,
          CreatedBy = createdBy,
          CreatedAtUtc = NowUtc
      };

    private static TimesheetCodeTransition NewTransition(
        Guid fromId,
        Guid toId,
        int shift,
        string createdBy = "seed")
        => new()
        {
            Id = Guid.NewGuid(),
            FromCodeId = fromId,
            ToCodeId = toId,
            StartShiftDays = shift,
            CreatedBy = createdBy,
            CreatedAtUtc = NowUtc
        };

    private static readonly string[] expected = ["Т", "A", "30", "100"];

    // ============================================================
    // GetCodesAsync
    // ============================================================

    [Fact]
    public async Task GetCodesAsync_by_default_returns_only_active_sorted()
    {
        await using var tdb = new SqliteTestDb();

        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var c1 = NewCode("30", "Базовий", sortOrder: 20, priority: 20, isActive: true);
        var c2 = NewCode("Т", "Тил", sortOrder: 10, priority: 10, isActive: true);
        var c3 = NewCode("ZZ", "Неактивний", sortOrder: 5, priority: 1, isActive: false);
        var c4 = NewCode("100", "Завдання", sortOrder: 110, priority: 110, isActive: true);
        var c5 = NewCode("A", "A", sortOrder: 20, priority: 10, isActive: true); // sortOrder same as 30, lower priority

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(c1, c2, c3, c4, c5);
            await db.SaveChangesAsync();
        }

        var res = await repo.GetCodesAsync(); // includeInactive=false

        // c3 excluded
        Assert.DoesNotContain(res, x => x.Code == "ZZ");

        // Sorted: SortOrder -> Priority -> Code
        Assert.Equal(expected, res.Select(x => x.Code).ToArray());
    }

    [Fact]
    public async Task GetCodesAsync_includeInactive_true_returns_all()
    {
        await using var tdb = new SqliteTestDb();

        var repo = new TimesheetPolicyRepository(tdb.Factory);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(
                NewCode("A", "A", 1, 1, isActive: true),
                NewCode("B", "B", 2, 1, isActive: false));
            await db.SaveChangesAsync();
        }

        var res = await repo.GetCodesAsync(includeInactive: true);

        Assert.Equal(2, res.Count);
        Assert.Contains(res, x => x.Code == "A");
        Assert.Contains(res, x => x.Code == "B");
    }

    // ============================================================
    // GetCodeByIdAsync
    // ============================================================

    [Fact]
    public async Task GetCodeByIdAsync_returns_null_when_missing()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var res = await repo.GetCodeByIdAsync(Guid.NewGuid());
        Assert.Null(res);
    }

    [Fact]
    public async Task GetCodeByIdAsync_throws_when_empty_id()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        await Assert.ThrowsAsync<ArgumentException>(() => repo.GetCodeByIdAsync(Guid.Empty));
    }

    // ============================================================
    // AddCodeAsync / CloseCodeAsync
    // ============================================================

    [Fact]
    public async Task AddCodeAsync_creates_code_and_trims_and_sets_audit()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var id = await repo.AddCodeAsync(
            code: "  X1  ",
            title: "  Назва  ",
            description: "  Опис  ",
            sortOrder: 5,
            priority: 7,
            isTerminal: true,
            author: "tester",
            nowUtc: NowUtc);

        await using var db = tdb.Factory.CreateDbContext();
        var stored = await db.TimesheetCodes.AsNoTracking().FirstAsync(x => x.Id == id);

        Assert.Equal("X1", stored.Code);
        Assert.Equal("Назва", stored.Title);
        Assert.Equal("Опис", stored.Description);
        Assert.Equal(5, stored.SortOrder);
        Assert.Equal(7, stored.Priority);
        Assert.True(stored.IsTerminal);
        Assert.True(stored.IsActive);
        Assert.Equal("tester", stored.CreatedBy);
        Assert.Equal(NowUtc, stored.CreatedAtUtc);
    }

    [Fact]
    public async Task AddCodeAsync_throws_when_duplicate_code_exists()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(NewCode("DUP", "dup", 1, 1, isActive: true));
            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.AddCodeAsync(
            code: "DUP",
            title: "x",
            description: null,
            sortOrder: 1,
            priority: 1,
            isTerminal: false,
            author: "tester",
            nowUtc: NowUtc));

        Assert.Contains("вже існує", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CloseCodeAsync_marks_inactive_and_sets_updated()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var code = NewCode("CL", "Close", 1, 1, isActive: true);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(code);
            await db.SaveChangesAsync();
        }

        await repo.CloseCodeAsync(code.Id, author: "admin", nowUtc: NowUtc);

        await using var db2 = tdb.Factory.CreateDbContext();
        var stored = await db2.TimesheetCodes.AsNoTracking().FirstAsync(x => x.Id == code.Id);

        Assert.False(stored.IsActive);
        Assert.Equal("admin", stored.UpdatedBy);
        Assert.Equal(NowUtc, stored.UpdatedAtUtc);
    }

    // ============================================================
    // GetAllowedTransitionsAsync
    // ============================================================

    [Fact]
    public async Task GetAllowedTransitionsAsync_returns_only_from_code_sorted_by_target()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var from = NewCode("30", "База", 20, 20);
        var toA = NewCode("A", "A", 10, 5);
        var toB = NewCode("B", "B", 10, 7);
        var toC = NewCode("C", "C", 30, 1);

        var otherFrom = NewCode("T", "T", 1, 1);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(from, toA, toB, toC, otherFrom);

            db.TimesheetCodeTransitions.AddRange(
                NewTransition(from.Id, toC.Id, 0),
                NewTransition(from.Id, toA.Id, 1),
                NewTransition(from.Id, toB.Id, 0),
                NewTransition(otherFrom.Id, toC.Id, 0) // чужий from
            );

            await db.SaveChangesAsync();
        }

        var res = await repo.GetAllowedTransitionsAsync(from.Id);

        Assert.Equal(3, res.Count);

        // Sorted by ToCode.SortOrder -> ToCode.Priority -> ToCode.Code
        Assert.Equal([toA.Id, toB.Id, toC.Id], [.. res.Select(x => x.ToCodeId)]);
    }

    // ============================================================
    // SavePolicyAsync
    // ============================================================

    [Fact]
    public async Task SavePolicyAsync_updates_code_fields_but_keeps_Code_and_created_audit()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var code = NewCode("30", "Old", 20, 20, isActive: true, isTerminal: false, createdBy: "seed");
        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.Add(code);
            await db.SaveChangesAsync();
        }

        await repo.SavePolicyAsync(
            codeId: code.Id,
            title: " New title ",
            description: " New desc ",
            sortOrder: 99,
            priority: 77,
            isTerminal: true,
            allowedTransitions: [],
            author: "admin",
            nowUtc: NowUtc);

        await using var db2 = tdb.Factory.CreateDbContext();
        var stored = await db2.TimesheetCodes.AsNoTracking().FirstAsync(x => x.Id == code.Id);

        Assert.Equal("30", stored.Code);                  // unchanged
        Assert.Equal("New title", stored.Title);
        Assert.Equal("New desc", stored.Description);
        Assert.Equal(99, stored.SortOrder);
        Assert.Equal(77, stored.Priority);
        Assert.True(stored.IsTerminal);

        Assert.Equal("seed", stored.CreatedBy);           // unchanged
        Assert.Equal(NowUtc, stored.UpdatedAtUtc);
        Assert.Equal("admin", stored.UpdatedBy);
    }

    [Fact]
    public async Task SavePolicyAsync_transitions_diff_update_add_remove_and_preserve_created_metadata()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var from = NewCode("30", "From", 1, 1);
        var to1 = NewCode("A", "A", 1, 1);
        var to2 = NewCode("B", "B", 2, 1);
        var to3 = NewCode("C", "C", 3, 1);

        var oldCreatedAt = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Utc);

        var tr1 = new TimesheetCodeTransition
        {
            Id = Guid.NewGuid(),
            FromCodeId = from.Id,
            ToCodeId = to1.Id,
            StartShiftDays = 0,
            CreatedBy = "seed",
            CreatedAtUtc = oldCreatedAt
        };

        var tr2 = NewTransition(from.Id, to2.Id, 1, createdBy: "seed");

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(from, to1, to2, to3);
            db.TimesheetCodeTransitions.AddRange(tr1, tr2);
            await db.SaveChangesAsync();
        }

        // desired: keep to1 but shift changes; remove to2; add to3
        await repo.SavePolicyAsync(
            codeId: from.Id,
            title: from.Title,
            description: null,
            sortOrder: from.SortOrder,
            priority: from.Priority,
            isTerminal: from.IsTerminal,
            allowedTransitions:
            [
                new TimesheetTransitionSpecDto(to1.Id, 1), // update shift
                new TimesheetTransitionSpecDto(to3.Id, 0)  // new
            ],
            author: "admin",
            nowUtc: NowUtc);

        await using var db2 = tdb.Factory.CreateDbContext();
        var stored = await db2.TimesheetCodeTransitions.AsNoTracking()
            .Where(x => x.FromCodeId == from.Id)
            .ToListAsync();

        Assert.Equal(2, stored.Count);

        var s1 = stored.Single(x => x.ToCodeId == to1.Id);
        Assert.Equal(1, s1.StartShiftDays);
        Assert.Equal("seed", s1.CreatedBy);           // preserved
        Assert.Equal(oldCreatedAt, s1.CreatedAtUtc);  // preserved

        var s3 = stored.Single(x => x.ToCodeId == to3.Id);
        Assert.Equal(0, s3.StartShiftDays);
        Assert.Equal("admin", s3.CreatedBy);
        Assert.Equal(NowUtc, s3.CreatedAtUtc);

        Assert.DoesNotContain(stored, x => x.ToCodeId == to2.Id); // removed
    }

    [Fact]
    public async Task SavePolicyAsync_throws_when_transition_target_is_inactive()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var from = NewCode("30", "From", 1, 1);
        var toInactive = NewCode("X", "X", 2, 1, isActive: false);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(from, toInactive);
            await db.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SavePolicyAsync(
            codeId: from.Id,
            title: "From",
            description: null,
            sortOrder: 1,
            priority: 1,
            isTerminal: false,
            allowedTransitions: [new TimesheetTransitionSpecDto(toInactive.Id, 0)],
            author: "admin",
            nowUtc: NowUtc));

        Assert.Contains("не існують або вже закриті", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SavePolicyAsync_throws_when_shift_out_of_range()
    {
        await using var tdb = new SqliteTestDb();
        var repo = new TimesheetPolicyRepository(tdb.Factory);

        var from = NewCode("30", "From", 1, 1);
        var to = NewCode("A", "A", 2, 1);

        using (var db = tdb.Factory.CreateDbContext())
        {
            db.TimesheetCodes.AddRange(from, to);
            await db.SaveChangesAsync();
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.SavePolicyAsync(
            codeId: from.Id,
            title: "From",
            description: null,
            sortOrder: 1,
            priority: 1,
            isTerminal: false,
            allowedTransitions: [new TimesheetTransitionSpecDto(to.Id, 99)], // out of allowed range
            author: "admin",
            nowUtc: NowUtc));
    }
}
