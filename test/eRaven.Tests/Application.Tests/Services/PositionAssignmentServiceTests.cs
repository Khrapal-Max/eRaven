//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PositionAssignmentServiceTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PositionAssignmentServiceTests : IDisposable
{
    private readonly SqliteDbHelper _dbh;
    private readonly PositionAssignmentService _svc;

    public PositionAssignmentServiceTests()
    {
        _dbh = new SqliteDbHelper();
        _svc = new PositionAssignmentService(_dbh.Factory);
    }

    public void Dispose() => _dbh.Dispose();

    // ----------------- helpers -----------------

    private async Task<Person> SeedPersonAsync(
        string rnokpp = "1111111111",
        string rank = "рядовий",
        string last = "Тест",
        string first = "Іван",
        string? middle = null)
    {
        var p = new Person
        {
            Id = Guid.NewGuid(),
            Rnokpp = rnokpp,
            Rank = rank,
            LastName = last,
            FirstName = first,
            MiddleName = middle,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow
        };
        _dbh.Db.Persons.Add(p);
        await _dbh.Db.SaveChangesAsync();
        return p;
    }

    private async Task<PositionUnit> SeedPositionAsync(
        string code = "INF-001",
        string shortName = "Стрілець",
        string orgPath = "Рота 1",
        string number = "999",
        bool isActive = true)
    {
        var u = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = code,
            ShortName = shortName,
            OrgPath = orgPath,
            SpecialNumber = number,
            IsActived = isActive
        };
        _dbh.Db.PositionUnits.Add(u);
        await _dbh.Db.SaveChangesAsync();
        return u;
    }

    private static DateTime Utc(int y, int M, int d, int h = 0, int m = 0) =>
        DateTime.SpecifyKind(new DateTime(y, M, d, h, m, 0), DateTimeKind.Utc);

    // ----------------- tests: read -----------------

    [Fact(DisplayName = "GetHistoryAsync: повертає до limit записів відсортованих за OpenUtc DESC")]
    public async Task GetHistory_ReturnsLimitedSorted()
    {
        var person = await SeedPersonAsync();
        var pos = await SeedPositionAsync();

        var baseUtc = Utc(2025, 9, 1, 8, 0);
        // 10 записів історії
        for (int i = 0; i < 10; i++)
        {
            _dbh.Db.PersonPositionAssignments.Add(new PersonPositionAssignment
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                PositionUnitId = pos.Id,
                OpenUtc = baseUtc.AddHours(i),
                CloseUtc = baseUtc.AddHours(i + 1),
                ModifiedUtc = DateTime.UtcNow
            });
        }
        await _dbh.Db.SaveChangesAsync();

        var list = await _svc.GetHistoryAsync(person.Id, limit: 5);

        Assert.Equal(5, list.Count);
        // Перевірка DESC
        Assert.True(list[0].OpenUtc > list[1].OpenUtc);
        Assert.True(list[1].OpenUtc > list[2].OpenUtc);
    }

    [Fact(DisplayName = "GetActiveAsync: повертає єдиний активний запис (CloseUtc = null)")]
    public async Task GetActive_ReturnsActive()
    {
        var person = await SeedPersonAsync();
        var pos1 = await SeedPositionAsync("INF-010", "Оператор", "Взвод 1");
        var pos2 = await SeedPositionAsync("INF-011", "Навідник", "Взвод 2");

        // минулий закритий
        _dbh.Db.PersonPositionAssignments.Add(new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = pos1.Id,
            OpenUtc = Utc(2025, 9, 1, 8, 0),
            CloseUtc = Utc(2025, 9, 2, 8, 0),
            ModifiedUtc = DateTime.UtcNow
        });

        // активний
        var active = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = pos2.Id,
            OpenUtc = Utc(2025, 9, 3, 9, 0),
            CloseUtc = null,
            ModifiedUtc = DateTime.UtcNow
        };
        _dbh.Db.PersonPositionAssignments.Add(active);
        await _dbh.Db.SaveChangesAsync();

        var got = await _svc.GetActiveAsync(person.Id);
        Assert.NotNull(got);
        Assert.Equal(active.Id, got!.Id);
        Assert.Null(got.CloseUtc);
        Assert.NotNull(got.PositionUnit);
        Assert.Equal(pos2.Id, got.PositionUnit.Id);
    }

    // ----------------- tests: assign -----------------

    [Fact(DisplayName = "AssignAsync: успіх — закриває попередній активний, створює новий, оновлює person.PositionUnitId")]
    public async Task Assign_Succeeds_ClosesPrevAndUpdatesPersonPointer()
    {
        var person = await SeedPersonAsync();
        var posOld = await SeedPositionAsync("INF-020", "Старе", "Взвод А");
        var posNew = await SeedPositionAsync("INF-021", "Нове", "Взвод Б");

        // актив до призначення
        var openOld = Utc(2025, 9, 1, 8, 0);
        var active = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = posOld.Id,
            OpenUtc = openOld,
            CloseUtc = null,
            ModifiedUtc = DateTime.UtcNow
        };
        _dbh.Db.PersonPositionAssignments.Add(active);

        // симулюємо, що у картці вказаний pointer на стару посаду
        person.PositionUnitId = posOld.Id;
        _dbh.Db.Persons.Update(person);

        await _dbh.Db.SaveChangesAsync();

        // нове призначення
        var openNew = Utc(2025, 9, 5, 12, 0);
        var created = await _svc.AssignAsync(person.Id, posNew.Id, openNew, "  Примітка  ");

        // старий актив мусив закритися рівно в openNew
        var closedOld = await _dbh.Db.PersonPositionAssignments.AsNoTracking()
            .FirstAsync(a => a.Id == active.Id);
        Assert.Equal(openNew, closedOld.CloseUtc);

        // новий запис створено як активний
        Assert.NotNull(created);
        Assert.Equal(person.Id, created.PersonId);
        Assert.Equal(posNew.Id, created.PositionUnitId);
        Assert.Equal(openNew, created.OpenUtc);
        Assert.Null(created.CloseUtc);
        Assert.Equal("Примітка", created.Note);
        Assert.NotNull(created.PositionUnit);
        Assert.Equal(posNew.Id, created.PositionUnit.Id);

        // pointer у Person оновлено
        var fromDbPerson = await _dbh.Db.Persons.AsNoTracking().FirstAsync(p => p.Id == person.Id);
        Assert.Equal(posNew.Id, fromDbPerson.PositionUnitId);
    }

    [Fact(DisplayName = "AssignAsync: кидає, якщо особи не існує")]
    public async Task Assign_Throws_WhenPersonNotFound()
    {
        var pos = await SeedPositionAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.AssignAsync(Guid.NewGuid(), pos.Id, Utc(2025, 9, 1), null));
    }

    [Fact(DisplayName = "AssignAsync: кидає, якщо посади не існує")]
    public async Task Assign_Throws_WhenPositionNotFound()
    {
        var person = await SeedPersonAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.AssignAsync(person.Id, Guid.NewGuid(), Utc(2025, 9, 1), null));
    }

    [Fact(DisplayName = "AssignAsync: заборона призначення на неактивну посаду")]
    public async Task Assign_Throws_WhenPositionInactive()
    {
        var person = await SeedPersonAsync();
        var inactive = await SeedPositionAsync(code: "INF-ZZ", isActive: false);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.AssignAsync(person.Id, inactive.Id, Utc(2025, 9, 1), null));
        Assert.Contains("неактивну", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AssignAsync: заборона, якщо посада вже зайнята")]
    public async Task Assign_Throws_WhenPositionOccupied()
    {
        var p1 = await SeedPersonAsync("2222222222", last: "А");
        var p2 = await SeedPersonAsync("3333333333", last: "Б");
        var pos = await SeedPositionAsync();

        // займаємо посаду p1
        _dbh.Db.PersonPositionAssignments.Add(new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p1.Id,
            PositionUnitId = pos.Id,
            OpenUtc = Utc(2025, 9, 1, 8, 0),
            CloseUtc = null,
            ModifiedUtc = DateTime.UtcNow
        });
        await _dbh.Db.SaveChangesAsync();

        // намагаємось призначити p2 на ту ж посаду
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.AssignAsync(p2.Id, pos.Id, Utc(2025, 9, 2, 8, 0), null));
        Assert.Contains("вже зайнята", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AssignAsync: дата відкриття має бути пізніше за попереднє призначення")]
    public async Task Assign_Throws_WhenOpenBeforeActive()
    {
        var person = await SeedPersonAsync();
        var posOld = await SeedPositionAsync("INF-030", "Стара", "X");
        var posNew = await SeedPositionAsync("INF-031", "Нова", "Y");

        // існуючий актив з 10:00
        _dbh.Db.PersonPositionAssignments.Add(new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = posOld.Id,
            OpenUtc = Utc(2025, 9, 10, 10, 0),
            CloseUtc = null,
            ModifiedUtc = DateTime.UtcNow
        });
        await _dbh.Db.SaveChangesAsync();

        // пробуємо відкрити нове раніше (09:00)
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.AssignAsync(person.Id, posNew.Id, Utc(2025, 9, 10, 9, 0), null));
    }

    // ----------------- tests: unassign -----------------

    [Fact(DisplayName = "UnassignAsync: успіх — закриває активний запис та очищає Person.PositionUnitId")]
    public async Task Unassign_Succeeds_ClosesAndClearsPointer()
    {
        var person = await SeedPersonAsync();
        var pos = await SeedPositionAsync();

        // актив
        var open = Utc(2025, 9, 1, 8, 0);
        var active = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = pos.Id,
            OpenUtc = open,
            CloseUtc = null,
            ModifiedUtc = DateTime.UtcNow
        };
        _dbh.Db.PersonPositionAssignments.Add(active);

        // pointer у Person
        person.PositionUnitId = pos.Id;
        _dbh.Db.Persons.Update(person);

        await _dbh.Db.SaveChangesAsync();

        var closedAt = Utc(2025, 9, 2, 12, 0);
        var ok = await _svc.UnassignAsync(person.Id, closedAt, "  Причина  ");
        Assert.True(ok);

        var fromDbAssign = await _dbh.Db.PersonPositionAssignments.AsNoTracking()
            .FirstAsync(a => a.Id == active.Id);
        Assert.Equal(closedAt, fromDbAssign.CloseUtc);
        Assert.Equal("Причина", fromDbAssign.Note);

        var fromDbPerson = await _dbh.Db.Persons.AsNoTracking().FirstAsync(p => p.Id == person.Id);
        Assert.Null(fromDbPerson.PositionUnitId);
    }

    [Fact(DisplayName = "UnassignAsync: повертає false, якщо активного призначення немає")]
    public async Task Unassign_ReturnsFalse_WhenNoActive()
    {
        var person = await SeedPersonAsync();
        var ok = await _svc.UnassignAsync(person.Id, Utc(2025, 9, 2, 10, 0), null);
        Assert.False(ok);
    }

    [Fact(DisplayName = "UnassignAsync: дата закриття має бути пізніше дати відкриття")]
    public async Task Unassign_Throws_WhenCloseBeforeOpen()
    {
        var person = await SeedPersonAsync();
        var pos = await SeedPositionAsync();

        // актив 12:00
        var open = Utc(2025, 9, 1, 12, 0);
        _dbh.Db.PersonPositionAssignments.Add(new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            PositionUnitId = pos.Id,
            OpenUtc = open,
            CloseUtc = null,
            ModifiedUtc = DateTime.UtcNow
        });
        await _dbh.Db.SaveChangesAsync();

        // закрити раніше 11:00 → помилка
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.UnassignAsync(person.Id, Utc(2025, 9, 1, 11, 0), null));
    }
}
