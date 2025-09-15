// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanServiceTests — інтеграційні тести сервісу на SQLite :memory:
// -----------------------------------------------------------------------------

using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public class PlanServiceTests : IDisposable
{
    private readonly SqliteDbHelper _dbh;
    private readonly PlanService _svc;

    // IDs згідно з Seed: 1 = "В районі", 2 = "В БР"
    private const int STATUS_IN_AREA = 1;
    private const int STATUS_BR = 2;

    private const string AUTHOR = "test";

    public PlanServiceTests()
    {
        _dbh = new SqliteDbHelper();
        _svc = new PlanService(_dbh.Db);
    }

    public void Dispose()
    {
        _dbh.Dispose();
        GC.SuppressFinalize(this);
    }

    private static (Person person, PositionUnit pos) NewPersonWithPosition()
    {
        var pos = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = "POS-1",
            ShortName = "Стрілець",
            OrgPath = "Взвод 1 / Рота 2",
            SpecialNumber = "123-45",
            IsActived = true
        };
        var p = new Person
        {
            Id = Guid.NewGuid(),
            Rnokpp = "1234567890",
            Rank = "Сержант",
            LastName = "Іваненко",
            FirstName = "Іван",
            MiddleName = "Іванович",
            BZVP = "так",
            PositionUnitId = pos.Id,
            PositionUnit = pos,
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow
        };
        return (p, pos);
    }

    private static PlanActionViewModel MakeVm(string planNum, Guid personId, PlanActionType type, DateTime whenUtc)
        => new()
        {
            PlanNumber = planNum,
            PersonId = personId,
            ActionType = type,
            EventAtUtc = whenUtc,
            Location = "Локація-A",
            GroupName = "Група-1",
            CrewName = "Екіпаж-A",
            Note = "тест"
        };

    // ----------------------- TESTS -----------------------

    [Fact]
    public async Task Dispatch_CreatesParticipant_And_Status()
    {
        var db = _dbh.Db;

        // Arrange
        var (person, pos) = NewPersonWithPosition();
        db.Persons.Add(person);
        db.PositionUnits.Add(pos);
        await db.SaveChangesAsync();

        var vm = MakeVm("PLN-001", person.Id, PlanActionType.Dispatch, DateTime.UtcNow);

        // Act
        var action = await _svc.AddActionAndApplyStatusAsync(vm, AUTHOR, CancellationToken.None);

        // Assert: action записаний
        Assert.Equal(PlanActionType.Dispatch, action.ActionType);
        Assert.Equal(person.Id, action.PersonId);

        // Assert: створений/існуючий учасник
        var pp = await db.PlanParticipants.SingleAsync();
        Assert.Equal(person.FullName, pp.FullName);
        Assert.Equal(person.Rank, pp.RankName);
        Assert.Equal(pos.ShortName, pp.PositionName);
        Assert.Equal(pos.OrgPath, pp.UnitName);

        // Assert: статус виставлено
        var ps = await db.PersonStatuses.SingleAsync();
        Assert.Equal(person.Id, ps.PersonId);
        Assert.Equal(STATUS_BR, ps.StatusKindId); // Dispatch -> "В БР"

        // Assert: у Person оновився поточний статус
        var pReload = await db.Persons.FindAsync(person.Id);
        Assert.Equal(STATUS_BR, pReload!.StatusKindId);
    }

    [Fact]
    public async Task Return_Without_Dispatch_Throws()
    {
        var db = _dbh.Db;
        var (person, pos) = NewPersonWithPosition();
        db.Persons.Add(person);
        db.PositionUnits.Add(pos);
        await db.SaveChangesAsync();

        var vm = MakeVm("PLN-002", person.Id, PlanActionType.Return, DateTime.UtcNow);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _svc.AddActionAndApplyStatusAsync(vm, AUTHOR, CancellationToken.None)
        );

        // Нічого не має бути записано
        Assert.Equal(0, await db.PlanParticipantActions.CountAsync());
        Assert.Equal(0, await db.PersonStatuses.CountAsync());
    }

    [Fact]
    public async Task Dispatch_Then_Dispatch_Throws()
    {
        var db = _dbh.Db;
        var (person, pos) = NewPersonWithPosition();
        db.Persons.Add(person);
        db.PositionUnits.Add(pos);
        await db.SaveChangesAsync();

        var t0 = DateTime.UtcNow.AddHours(-1);
        var t1 = DateTime.UtcNow;

        // Перша дія: Dispatch
        await _svc.AddActionAndApplyStatusAsync(MakeVm("PLN-003", person.Id, PlanActionType.Dispatch, t0), AUTHOR, CancellationToken.None);

        // Друга дія: знову Dispatch (має впасти)
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _svc.AddActionAndApplyStatusAsync(MakeVm("PLN-003", person.Id, PlanActionType.Dispatch, t1), AUTHOR, CancellationToken.None)
        );

        // Перевіримо, що лише 1 дія та 1 статус
        Assert.Equal(1, await db.PlanParticipantActions.CountAsync());
        Assert.Equal(1, await db.PersonStatuses.CountAsync());
    }

    [Fact]
    public async Task Dispatch_Then_Return_WritesTwoStatuses_And_UpdatesPerson()
    {
        var db = _dbh.Db;
        var (person, pos) = NewPersonWithPosition();
        db.Persons.Add(person);
        db.PositionUnits.Add(pos);
        await db.SaveChangesAsync();

        var t0 = DateTime.UtcNow.AddHours(-2);
        var t1 = DateTime.UtcNow;

        // Dispatch
        await _svc.AddActionAndApplyStatusAsync(MakeVm("PLN-004", person.Id, PlanActionType.Dispatch, t0), AUTHOR, CancellationToken.None);

        // Return
        await _svc.AddActionAndApplyStatusAsync(MakeVm("PLN-004", person.Id, PlanActionType.Return, t1), AUTHOR, CancellationToken.None);

        // Дві дії
        Assert.Equal(2, await db.PlanParticipantActions.CountAsync());

        // Два статуси
        var statuses = await db.PersonStatuses.OrderBy(s => s.OpenDate).ToListAsync();
        Assert.Equal(2, statuses.Count);
        Assert.Equal(STATUS_BR, statuses[0].StatusKindId); // перший — В БР
        Assert.Equal(STATUS_IN_AREA, statuses[1].StatusKindId); // другий — В районі

        // Актуальний у Person — "В районі"
        var pReload = await db.Persons.FindAsync(person.Id);
        Assert.Equal(STATUS_IN_AREA, pReload!.StatusKindId);
    }

    [Fact]
    public async Task Batch_Applies_PerPerson_In_Time_Order()
    {
        var db = _dbh.Db;

        var (p1, pos1) = NewPersonWithPosition();
        var (p2, pos2) = NewPersonWithPosition();
        p1.Rnokpp = "1234567890";
        p2.Rnokpp = "1234567891";
        db.AddRange(p1, pos1, p2, pos2);
        await db.SaveChangesAsync();

        var planNum = "PLN-005";
        var now = DateTime.UtcNow;
        var vm = new PlanBatchViewModel
        {
            PlanNumber = planNum,
            Actions =
            [
                // p1: Dispatch -> Return
                new PlanActionViewModel { PlanNumber = planNum, PersonId = p1.Id, ActionType = PlanActionType.Dispatch, EventAtUtc = now.AddHours(-3), Location = "L1", GroupName = "G1", CrewName = "C1" },
                new PlanActionViewModel { PlanNumber = planNum, PersonId = p1.Id, ActionType = PlanActionType.Return,   EventAtUtc = now.AddHours(-1), Location = "L1", GroupName = "G1", CrewName = "C1" },

                // p2: тільки Dispatch
                new PlanActionViewModel { PlanNumber = planNum, PersonId = p2.Id, ActionType = PlanActionType.Dispatch, EventAtUtc = now.AddHours(-2), Location = "L2", GroupName = "G2", CrewName = "C2" },
            ]
        };

        await _svc.ApplyBatchAsync(vm, AUTHOR, CancellationToken.None);

        // p1 → 2 дії, p2 → 1 дія
        var actions = await db.PlanParticipantActions.OrderBy(a => a.PersonId).ThenBy(a => a.EventAtUtc).ToListAsync();
        Assert.Equal(3, actions.Count);

        // Перевіримо порядок по p1
        var p1Acts = actions.Where(a => a.PersonId == p1.Id).ToList();
        Assert.Equal(2, p1Acts.Count);
        Assert.Equal(PlanActionType.Dispatch, p1Acts[0].ActionType);
        Assert.Equal(PlanActionType.Return, p1Acts[1].ActionType);
        Assert.True(p1Acts[0].EventAtUtc < p1Acts[1].EventAtUtc);

        // Статуси: 3 записи
        Assert.Equal(3, await db.PersonStatuses.CountAsync());

        // Поточні статуси:
        Assert.Equal(STATUS_IN_AREA, (await db.Persons.FindAsync(p1.Id))!.StatusKindId);
        Assert.Equal(STATUS_BR, (await db.Persons.FindAsync(p2.Id))!.StatusKindId);
    }
}
