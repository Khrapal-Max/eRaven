//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskReadRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Tests.Extensions;

namespace eRaven.Tests.Infrastructure.Repositories;

public sealed class CombatTaskReadRepositoryTests
{
    [Fact]
    public async Task GetPlanningMonthAsync_ReturnsOverlapRows_AndSkipsVoidedDocuments()
    {
        await using var testDb = new SqliteTestDb();

        var docDraft = new CombatTaskPlanDocument
        {
            Id = Guid.NewGuid(),
            PlanningDate = new DateOnly(2026, 1, 10),
            PlanningDocTitle = "План Alpha",
            Status = CombatTaskPlanDocumentStatus.Draft,
            RecordedAt = new DateOnly(2026, 1, 10),
            CreatedBy = "test",
            CreatedAtUtc = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc)
        };

        var docVoided = new CombatTaskPlanDocument
        {
            Id = Guid.NewGuid(),
            PlanningDate = new DateOnly(2026, 1, 12),
            PlanningDocTitle = "План VOID",
            Status = CombatTaskPlanDocumentStatus.Canceled,
            RecordedAt = new DateOnly(2026, 1, 12),
            CreatedBy = "test",
            CreatedAtUtc = new DateTime(2026, 1, 12, 8, 0, 0, DateTimeKind.Utc)
        };

        var p1 = Guid.NewGuid();
        var a1 = new CombatTaskAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p1,

            PlanningDate = new DateOnly(2026, 1, 10),
            PlanningDocTitle = "План Alpha",

            StartedAt = new DateOnly(2026, 1, 15),
            EndedAt = null,

            PositionalArea = "Район-1",
            GroupName = "Група-A",
            AssetType = "FPV",
            Mode = CombatTaskMode.Day,
            Goal = "Розвідка",

            RNOKPP = "111",
            FullName = "Іванов Іван",
            Rank = "солдат",
            Position = "оператор",
            Weapon = "АК",
            Callsign = "FOX",

            StartDocumentId = docDraft.Id,

            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

        var aVoided = new CombatTaskAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),

            PlanningDate = new DateOnly(2026, 1, 12),
            PlanningDocTitle = "План VOID",

            StartedAt = new DateOnly(2026, 1, 18),
            EndedAt = null,

            PositionalArea = "Район-9",
            GroupName = "Група-Z",
            AssetType = null,
            Mode = CombatTaskMode.Night,
            Goal = "Інше",

            RNOKPP = "999",
            FullName = "Петров Петро",

            StartDocumentId = docVoided.Id,

            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

        await using (var db = testDb.Factory.CreateDbContext())
        {
            db.CombatTaskPlanDocuments.AddRange(docDraft, docVoided);
            db.CombatTaskAssignments.AddRange(a1, aVoided);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskReadRepository(testDb.Factory);

        var rows = await repo.GetPlanningMonthAsync(2026, 1, search: null);

        Assert.Single(rows);
        Assert.Equal(a1.Id, rows[0].AssignmentId);
        Assert.Equal(CombatTaskPlanDocumentStatus.Draft, rows[0].DocumentStatus);
        Assert.Equal("Район-1", rows[0].PositionalArea);
        Assert.True(rows[0].IsActual);
    }

    [Fact]
    public async Task GetPlanningDayAsync_GroupsByKeys_AndReturnsPersons()
    {
        await using var testDb = new SqliteTestDb();

        var doc = new CombatTaskPlanDocument
        {
            Id = Guid.NewGuid(),
            PlanningDate = new DateOnly(2026, 1, 20),
            PlanningDocTitle = "План Bravo",
            Status = CombatTaskPlanDocumentStatus.Posted,
            RecordedAt = new DateOnly(2026, 1, 20),
            CreatedBy = "test",
            CreatedAtUtc = new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Utc)
        };

        var p1 = Guid.NewGuid();
        var a1 = new CombatTaskAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p1,

            PlanningDate = new DateOnly(2026, 1, 20),
            PlanningDocTitle = "План Bravo",

            StartedAt = new DateOnly(2026, 1, 21),
            EndedAt = new DateOnly(2026, 1, 23),

            PositionalArea = "Район-2",
            GroupName = "Група-B",
            AssetType = "Мавік",
            Mode = CombatTaskMode.FullDay,
            Goal = "Спостереження",

            RNOKPP = "222",
            FullName = "Сидоренко Сидір",
            Rank = "сержант",
            Position = "командир",
            Callsign = "EAGLE",

            StartDocumentId = doc.Id,

            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

        await using (var db = testDb.Factory.CreateDbContext())
        {
            db.CombatTaskPlanDocuments.Add(doc);
            db.CombatTaskAssignments.Add(a1);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskReadRepository(testDb.Factory);

        var day = new DateOnly(2026, 1, 22);
        var groups = await repo.GetPlanningDayAsync(day, search: null);

        Assert.Single(groups);

        var g = groups[0];
        Assert.Equal("Район-2", g.PositionalArea);
        Assert.Equal("Група-B", g.GroupName);
        Assert.Equal("Мавік", g.AssetType);
        Assert.Equal(CombatTaskMode.FullDay.ToString(), g.Mode);
        Assert.Equal("Спостереження", g.Goal);

        Assert.Single(g.Persons);
        Assert.Equal("222", g.Persons[0].RNOKPP);
        Assert.Equal(new DateOnly(2026, 1, 21), g.Persons[0].StartDate);
        Assert.Equal(new DateOnly(2026, 1, 23), g.Persons[0].EndDate);
    }

    [Fact]
    public async Task GetPlanningDocumentsAsync_ReturnsDocumentsForMonth_WithComputedCounts()
    {
        await using var testDb = new SqliteTestDb();

        var doc1 = new CombatTaskPlanDocument
        {
            Id = Guid.NewGuid(),
            PlanningDate = new DateOnly(2026, 1, 5),
            PlanningDocTitle = "План 1",
            Status = CombatTaskPlanDocumentStatus.Draft,
            RecordedAt = new DateOnly(2026, 1, 5),
            CreatedBy = "test",
            CreatedAtUtc = new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc)
        };

        var doc2 = new CombatTaskPlanDocument
        {
            Id = Guid.NewGuid(),
            PlanningDate = new DateOnly(2026, 1, 6),
            PlanningDocTitle = "План 2",
            Status = CombatTaskPlanDocumentStatus.Posted,
            RecordedAt = new DateOnly(2026, 1, 6),
            CreatedBy = "test",
            CreatedAtUtc = new DateTime(2026, 1, 6, 8, 0, 0, DateTimeKind.Utc)
        };

        var p1 = Guid.NewGuid();
        var a1 = new CombatTaskAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p1,
            PlanningDate = new DateOnly(2026, 1, 5),
            PlanningDocTitle = "План 1",
            StartedAt = new DateOnly(2026, 1, 10),
            EndedAt = null,
            PositionalArea = "Район",
            GroupName = "Група",
            Goal = "Ціль",
            RNOKPP = "333",
            FullName = "Коваленко",
            Mode = CombatTaskMode.Day,
            StartDocumentId = doc1.Id,
            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

        await using (var db = testDb.Factory.CreateDbContext())
        {
            db.CombatTaskPlanDocuments.AddRange(doc1, doc2);
            db.CombatTaskAssignments.Add(a1);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskReadRepository(testDb.Factory);

        var docs = await repo.GetPlanningDocumentsAsync(2026, 1, status: null, search: null);

        Assert.Equal(2, docs.Count);

        var d1 = docs.Single(x => x.DocumentId == doc1.Id);
        Assert.Equal(1, d1.PersonsCount);
        Assert.Equal(new DateOnly(2026, 1, 10), d1.MinStart);
        Assert.Null(d1.MaxEnd);

        var d2 = docs.Single(x => x.DocumentId == doc2.Id);
        Assert.Equal(0, d2.PersonsCount);
        Assert.Null(d2.MinStart);
        Assert.Null(d2.MaxEnd);
    }

    [Fact]
    public async Task GetPlanningMonthAsync_SearchFiltersByNameOrRnokpp()
    {
        await using var testDb = new SqliteTestDb();

        var doc = new CombatTaskPlanDocument
        {
            Id = Guid.NewGuid(),
            PlanningDate = new DateOnly(2026, 1, 10),
            PlanningDocTitle = "План Search",
            Status = CombatTaskPlanDocumentStatus.Posted,
            RecordedAt = new DateOnly(2026, 1, 10),
            CreatedBy = "test",
            CreatedAtUtc = new DateTime(2026, 1, 10, 8, 0, 0, DateTimeKind.Utc)
        };

        var a = new CombatTaskAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            PlanningDate = new DateOnly(2026, 1, 10),
            PlanningDocTitle = "План Search",
            StartedAt = new DateOnly(2026, 1, 12),
            EndedAt = null,
            PositionalArea = "Р",
            GroupName = "Г",
            Goal = "Ц",
            RNOKPP = "777777",
            FullName = "Тест Прізвище",
            Mode = CombatTaskMode.Day,
            StartDocumentId = doc.Id,
            CreatedBy = "test",
            CreatedAtUtc = DateTime.UtcNow
        };

        await using (var db = testDb.Factory.CreateDbContext())
        {
            db.CombatTaskPlanDocuments.Add(doc);
            db.CombatTaskAssignments.Add(a);
            await db.SaveChangesAsync();
        }

        var repo = new CombatTaskReadRepository(testDb.Factory);

        var byRnokpp = await repo.GetPlanningMonthAsync(2026, 1, "777");
        Assert.Single(byRnokpp);

        var byName = await repo.GetPlanningMonthAsync(2026, 1, "Прізвище");
        Assert.Single(byName);

        var none = await repo.GetPlanningMonthAsync(2026, 1, "zzz");
        Assert.Empty(none);
    }
}
