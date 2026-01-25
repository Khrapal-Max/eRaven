//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskAssignmentRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class CombatTaskAssignmentRepositoryTests
{
    private static readonly DateTime NowUtc = new(2026, 01, 25, 12, 00, 00, DateTimeKind.Utc);

    [Fact]
    public async Task GetOpenAssignmentForPersonAsync_when_none_returns_null()
    {
        await using var db = new SqliteTestDb();
        var repo = new CombatTaskAssignmentRepository(db.Factory);

        var personId = Guid.NewGuid();

        var open = await repo.GetOpenAssignmentForPersonAsync(personId);

        Assert.Null(open);
    }

    [Fact]
    public async Task CreateAssignmentAsync_should_persist_and_GetOpen_returns_it()
    {
        await using var db = new SqliteTestDb();
        var repo = new CombatTaskAssignmentRepository(db.Factory);

        var personId = Guid.NewGuid();
        var a = NewOpenAssignment(personId, startedAt: new DateOnly(2026, 1, 10));

        await repo.CreateAssignmentAsync(a);

        var open = await repo.GetOpenAssignmentForPersonAsync(personId);

        Assert.NotNull(open);
        Assert.Equal(a.Id, open!.Id);
        Assert.Equal(personId, open.PersonId);
        Assert.Equal(a.StartedAt, open.StartedAt);
        Assert.Null(open.EndedAt);
        Assert.Equal(a.StartDocumentId, open.StartDocumentId);
    }

    [Fact]
    public async Task CloseAssignmentAsync_should_set_EndedAt_and_EndDocument_and_remove_open()
    {
        await using var db = new SqliteTestDb();
        var repo = new CombatTaskAssignmentRepository(db.Factory);

        var personId = Guid.NewGuid();
        var a = NewOpenAssignment(personId, startedAt: new DateOnly(2026, 1, 10));

        await repo.CreateAssignmentAsync(a);

        var endDocId = Guid.NewGuid();
        var endedAt = new DateOnly(2026, 1, 12);

        await repo.CloseAssignmentAsync(
            assignmentId: a.Id,
            endedAt: endedAt,
            endDocumentId: endDocId,
            author: "tester",
            nowUtc: NowUtc);

        // open should be gone
        var open = await repo.GetOpenAssignmentForPersonAsync(personId);
        Assert.Null(open);

        // verify persisted changes directly via db
        await using var ctx = await db.Factory.CreateDbContextAsync();
        var stored = await ctx.Set<CombatTaskAssignment>()
            .AsNoTracking()
            .SingleAsync(x => x.Id == a.Id);

        Assert.Equal(endedAt, stored.EndedAt);
        Assert.Equal(endDocId, stored.EndDocumentId);
        Assert.Equal("tester", stored.UpdatedBy);
        Assert.Equal(NowUtc, stored.UpdatedAtUtc);
    }

    [Fact]
    public async Task CloseAssignmentAsync_when_not_found_should_throw()
    {
        await using var db = new SqliteTestDb();
        var repo = new CombatTaskAssignmentRepository(db.Factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.CloseAssignmentAsync(
            assignmentId: Guid.NewGuid(),
            endedAt: new DateOnly(2026, 1, 12),
            endDocumentId: Guid.NewGuid(),
            author: "tester",
            nowUtc: NowUtc));
    }

    [Fact]
    public async Task CloseAssignmentAsync_when_ended_before_started_should_throw()
    {
        await using var db = new SqliteTestDb();
        var repo = new CombatTaskAssignmentRepository(db.Factory);

        var personId = Guid.NewGuid();
        var a = NewOpenAssignment(personId, startedAt: new DateOnly(2026, 1, 10));
        await repo.CreateAssignmentAsync(a);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => repo.CloseAssignmentAsync(
            assignmentId: a.Id,
            endedAt: new DateOnly(2026, 1, 09), // before start
            endDocumentId: Guid.NewGuid(),
            author: "tester",
            nowUtc: NowUtc));

        Assert.Contains("раніше", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAssignmentAsync_when_missing_required_fields_should_throw()
    {
        await using var db = new SqliteTestDb();
        var repo = new CombatTaskAssignmentRepository(db.Factory);

        var a = NewOpenAssignment(Guid.NewGuid(), startedAt: new DateOnly(2026, 1, 10));
        a.RNOKPP = "   "; // violates repo validation (expected)

        await Assert.ThrowsAsync<ArgumentException>(() => repo.CreateAssignmentAsync(a));
    }

    /// <summary>
    /// Фіксує правило: НЕ може бути 2 відкритих завдання одночасно на 1 особу.
    /// Тест буде зеленим, якщо в EF-конфігу є partial unique index:
    ///   UNIQUE(PersonId) WHERE EndedAt IS NULL
    /// </summary>
    [Fact]
    public async Task CreateAssignmentAsync_when_second_open_for_same_person_should_fail()
    {
        await using var db = new SqliteTestDb();
        var repo = new CombatTaskAssignmentRepository(db.Factory);

        var personId = Guid.NewGuid();

        await repo.CreateAssignmentAsync(NewOpenAssignment(personId, new DateOnly(2026, 1, 10)));

        await Assert.ThrowsAnyAsync<DbUpdateException>(() =>
            repo.CreateAssignmentAsync(NewOpenAssignment(personId, new DateOnly(2026, 1, 11))));
    }

    // -----------------------
    // Helpers
    // -----------------------

    private static CombatTaskAssignment NewOpenAssignment(Guid personId, DateOnly startedAt)
        => new()
        {
            Id = Guid.NewGuid(),

            PersonId = personId,
            RNOKPP = "1234567890",
            FullName = "Тестовий Тест",
            Rank = "солдат",
            Position = "стрілець",
            Weapon = "АК-74",
            Callsign = "FOX",

            PlanningDate = new DateOnly(2026, 1, 25),
            PlanningDocTitle = "План №1",

            StartedAt = startedAt,
            EndedAt = null,

            PositionalArea = "Район-1",
            GroupName = "ГРУПА-А",
            AssetType = null,
            Mode = CombatTaskMode.Day,
            Goal = "Спостереження",

            IsActual = true,

            StartDocumentId = Guid.NewGuid(),
            EndDocumentId = null,

            CreatedBy = "seed",
            CreatedAtUtc = NowUtc.AddDays(-1)
        };
}
