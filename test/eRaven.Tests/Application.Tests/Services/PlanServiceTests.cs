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

public sealed class PlanServiceTests : IDisposable
{
    private readonly SqliteDbHelper _helper;
    private readonly PlanService _svc;

    public PlanServiceTests()
    {
        _helper = new SqliteDbHelper();
        _svc = new PlanService(_helper.Db);
    }

    public void Dispose() => _helper.Dispose();

    // --------------------------- helpers ---------------------------

    private static DateTime Utc(int y, int M, int d, int h, int m)
        => new(y, M, d, h, m, 0, DateTimeKind.Utc);

    private async Task<Person> SeedPersonAsync(string name = "Тест", string rnokpp = "1234567890")
    {
        var p = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = name,
            LastName = name,
            MiddleName = name,
            Rnokpp = rnokpp,
            Rank = "рядовий",
            Weapon = "АК",
            Callsign = "Тест"
            // PositionUnit/StatusKind можна не задавати — сервіс це допускає
        };

        _helper.Db.Persons.Add(p);
        await _helper.Db.SaveChangesAsync();
        return p;
    }

    private async Task<Plan> SeedPlanAsync(string number = "PL-001", PlanState state = PlanState.Open)
    {
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = number,
            State = state,
            Author = "test",
            RecordedUtc = DateTime.UtcNow
        };
        _helper.Db.Plans.Add(plan);
        await _helper.Db.SaveChangesAsync();
        return plan;
    }

    private async Task<PlanElement> SeedDispatchAsync(Guid planId, Person person, DateTime whenUtc,
        string? location = "СТЕПОВЕ", string? group = "МАЛІБУ", string? tool = "ФПВ", string? note = "seed")
    {
        var vm = new CreatePlanElementViewModel
        {
            Type = PlanType.Dispatch,
            EventAtUtc = whenUtc,
            Location = location,
            GroupName = group,
            ToolType = tool,
            Note = note,
            PersonId = person.Id
        };

        return await _svc.AddElementAsync(planId, vm);
    }

    // --------------------------- tests: read/create/delete plan ---------------------------

    [Fact(DisplayName = "GetAllPlansAsync / GetByIdAsync: план створюється і читається з елементами та PPS")]
    public async Task Read_Create_Plan_With_Includes()
    {
        // create empty plan
        var created = await _svc.CreateAsync(new CreatePlanViewModel { PlanNumber = "PL-READ", State = PlanState.Open });

        // add one element
        var person = await SeedPersonAsync();
        var when = Utc(2025, 9, 12, 12, 30);
        await SeedDispatchAsync(created.Id, person, when);

        // read list
        var all = await _svc.GetAllPlansAsync();
        Assert.Contains(all, p => p.Id == created.Id);

        // read by id with includes
        var loaded = await _svc.GetByIdAsync(created.Id);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded!.PlanElements);
        Assert.Single(loaded.PlanElements);

        var el = loaded.PlanElements.First();
        Assert.Equal(PlanType.Dispatch, el.Type);
        Assert.NotNull(el.PlanParticipantSnapshot);
        Assert.Equal(person.Id, el.PlanParticipantSnapshot.PersonId);
    }

    [Fact(DisplayName = "CreateAsync: унікальність номера плану — кинути помилку при дублі")]
    public async Task CreatePlan_UniqueNumber()
    {
        await _svc.CreateAsync(new CreatePlanViewModel { PlanNumber = "PL-UNIQ" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _svc.CreateAsync(new CreatePlanViewModel { PlanNumber = "PL-UNIQ" })
        );

        Assert.Contains("вже існує", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "DeleteIfOpenAsync: видаляє відкритий план")]
    public async Task Delete_Open_Plan()
    {
        var plan = await SeedPlanAsync("PL-DEL-OPEN", PlanState.Open);
        var ok = await _svc.DeleteIfOpenAsync(plan.Id);
        Assert.True(ok);

        var again = await _svc.GetByIdAsync(plan.Id);
        Assert.Null(again);
    }

    [Fact(DisplayName = "DeleteIfOpenAsync: кине помилку для закритого плану")]
    public async Task Delete_Closed_Plan_Throws()
    {
        // ⚠ якщо у вашому enum інша назва стану, підставте її
        var plan = await SeedPlanAsync("PL-DEL-CLOSED", state: PlanState.Close);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _svc.DeleteIfOpenAsync(plan.Id)
        );

        Assert.Contains("закритий", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --------------------------- tests: add element ---------------------------

    [Fact(DisplayName = "AddElementAsync: успішний Dispatch створює елемент і PPS")]
    public async Task Add_Dispatch_Success()
    {
        var plan = await SeedPlanAsync("PL-A1");
        var person = await SeedPersonAsync();
        var when = Utc(2025, 9, 12, 12, 30);

        var el = await _svc.AddElementAsync(plan.Id, new CreatePlanElementViewModel
        {
            Type = PlanType.Dispatch,
            EventAtUtc = when,
            Location = "СТЕПОВЕ",
            GroupName = "МАЛІБУ",
            ToolType = "ФПВ",
            Note = "перший",
            PersonId = person.Id
        });

        Assert.Equal(plan.Id, el.PlanId);
        Assert.Equal(PlanType.Dispatch, el.Type);
        Assert.Equal(when, el.EventAtUtc);
        Assert.Equal("СТЕПОВЕ", el.Location);
        Assert.NotNull(el.PlanParticipantSnapshot);
        Assert.Equal(person.Id, el.PlanParticipantSnapshot.PersonId);

        // verify persisted
        var reloaded = await _svc.GetByIdAsync(plan.Id);
        Assert.NotNull(reloaded);
        Assert.Single(reloaded!.PlanElements);
    }

    [Fact(DisplayName = "AddElementAsync: Return без попереднього Dispatch → помилка")]
    public async Task Add_Return_Without_Dispatch_Fails()
    {
        var plan = await SeedPlanAsync("PL-A2");
        var person = await SeedPersonAsync();
        var when = Utc(2025, 9, 12, 12, 30);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _svc.AddElementAsync(plan.Id, new CreatePlanElementViewModel
            {
                Type = PlanType.Return,
                EventAtUtc = when,
                PersonId = person.Id
            })
        );

        Assert.Contains("Повернення", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AddElementAsync: Return підтягує контекст з останнього Dispatch")]
    public async Task Add_Return_Uses_LastDispatch_Context()
    {
        var plan = await SeedPlanAsync("PL-A3");
        var person = await SeedPersonAsync();

        var t1 = Utc(2025, 9, 12, 12, 30);
        var t2 = Utc(2025, 9, 12, 15, 45);

        // seed dispatch
        await SeedDispatchAsync(plan.Id, person, t1, "СТЕПОВЕ", "АНІМЕ", "МАВІК");

        // add return (без контексту у VM)
        var r = await _svc.AddElementAsync(plan.Id, new CreatePlanElementViewModel
        {
            Type = PlanType.Return,
            EventAtUtc = t2,
            PersonId = person.Id
        });

        Assert.Equal(PlanType.Return, r.Type);
        Assert.Equal("СТЕПОВЕ", r.Location);
        Assert.Equal("АНІМЕ", r.GroupName);
        Assert.Equal("МАВІК", r.ToolType);
    }

    [Fact(DisplayName = "AddElementAsync: дубль на той самий момент (person|type|time) → помилка")]
    public async Task Add_Duplicate_Same_Time_And_Type_Fails()
    {
        var plan = await SeedPlanAsync("PL-A4");
        var person = await SeedPersonAsync();
        var when = Utc(2025, 9, 12, 12, 30);

        await SeedDispatchAsync(plan.Id, person, when);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await SeedDispatchAsync(plan.Id, person, when)
        );

        Assert.Contains("вже є така дія", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AddElementAsync: чергування дій — двічі Dispatch поспіль → помилка")]
    public async Task Add_Same_Action_Twice_In_A_Row_Fails()
    {
        var plan = await SeedPlanAsync("PL-A5");
        var person = await SeedPersonAsync();

        await SeedDispatchAsync(plan.Id, person, Utc(2025, 9, 12, 12, 30));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await SeedDispatchAsync(plan.Id, person, Utc(2025, 9, 12, 14, 30))
        );

        Assert.Contains("Попередня дія", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AddElementAsync: час не на 00/15/30/45 → помилка")]
    public async Task Add_Invalid_Time_Fails()
    {
        var plan = await SeedPlanAsync("PL-A6");
        var person = await SeedPersonAsync();

        var bad = new DateTime(2025, 9, 12, 12, 10, 0, DateTimeKind.Utc);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _svc.AddElementAsync(plan.Id, new CreatePlanElementViewModel
            {
                Type = PlanType.Dispatch,
                EventAtUtc = bad,
                Location = "X",
                PersonId = person.Id
            })
        );

        Assert.Contains("інтервалах 00/15/30/45", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "AddElementAsync: особа не існує → помилка")]
    public async Task Add_With_Unknown_Person_Fails()
    {
        var plan = await SeedPlanAsync("PL-A7");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _svc.AddElementAsync(plan.Id, new CreatePlanElementViewModel
            {
                Type = PlanType.Dispatch,
                EventAtUtc = Utc(2025, 9, 12, 12, 30),
                PersonId = Guid.NewGuid()
            })
        );

        Assert.Contains("Особу не знайдено", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    // --------------------------- tests: remove element ---------------------------

    [Fact(DisplayName = "RemoveElementAsync: видаляє елемент і повертає true")]
    public async Task Remove_Element_Success()
    {
        var plan = await SeedPlanAsync("PL-R1");
        var person = await SeedPersonAsync();
        var el = await SeedDispatchAsync(plan.Id, person, Utc(2025, 9, 12, 12, 30));

        var ok = await _svc.RemoveElementAsync(plan.Id, el.Id);
        Assert.True(ok);

        var re = await _svc.GetByIdAsync(plan.Id);
        Assert.NotNull(re);
        Assert.Empty(re!.PlanElements);
    }

    [Fact(DisplayName = "RemoveElementAsync: план закритий → помилка")]
    public async Task Remove_Element_From_Closed_Plan_Fails()
    {
        // ⚠ якщо у вашому enum інша назва стану, підставте її
        var plan = await SeedPlanAsync("PL-R2", PlanState.Close);
        var person = await SeedPersonAsync();

        // вручну додаємо елемент (через контекст), щоб не спотикатися об гвард AddElementAsync
        var el = new PlanElement
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Type = PlanType.Dispatch,
            EventAtUtc = Utc(2025, 9, 12, 12, 30),
            Location = "X",
            PlanParticipantSnapshot = new PlanParticipantSnapshot
            {
                Id = Guid.NewGuid(),
                PersonId = person.Id,
                FullName = person.FullName ?? "N/A",
                Rnokpp = person.Rnokpp ?? "0000000000",
                RecordedUtc = DateTime.UtcNow
            }
        };
        _helper.Db.PlanElements.Add(el);
        await _helper.Db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _svc.RemoveElementAsync(plan.Id, el.Id)
        );

        Assert.Contains("План закритий", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "RemoveElementAsync: елемент не знайдено → false")]
    public async Task Remove_Element_NotFound_ReturnsFalse()
    {
        var plan = await SeedPlanAsync("PL-R3", PlanState.Open);
        var ok = await _svc.RemoveElementAsync(plan.Id, Guid.NewGuid());
        Assert.False(ok);
    }
}
