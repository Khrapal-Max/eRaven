//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonServiceTests
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonService;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PersonServiceTests : IDisposable
{
    private readonly SqliteDbHelper _helper;

    public PersonServiceTests()
    {
        _helper = new SqliteDbHelper();
    }

    public void Dispose()
    {
        _helper.Dispose();
        GC.SuppressFinalize(this);
    }

    // ------------------------ helpers ------------------------

    private AppDbContext Ctx => _helper.Db;

    private static Person P(string ln, string fn, string? mn = null, string rnokpp = "0000000000") =>
        new()
        {
            Id = Guid.NewGuid(),
            LastName = ln,
            FirstName = fn,
            MiddleName = mn,
            Rnokpp = rnokpp,
            StatusKindId = 1
        };

    private async Task SeedAsync(params Person[] people)
    {
        // мінімальні довідники
        if (!await Ctx.StatusKinds.AnyAsync())
            Ctx.StatusKinds.Add(new StatusKind
            {
                Id = 1,
                Name = "В районі",
                Code = "RDY",
                Order = 1,
                IsActive = true
            });

        if (!await Ctx.PositionUnits.AnyAsync())
            Ctx.PositionUnits.Add(new PositionUnit
            {
                Id = Guid.NewGuid(),
                Code = "VAC-000",                // ← ОБОВ’ЯЗКОВО
                ShortName = "Вакант",
                OrgPath = "—"
            });

        if (people is { Length: > 0 })
            Ctx.Persons.AddRange(people);

        await Ctx.SaveChangesAsync();
    }

    private PersonService Svc() => new(Ctx);

    // ------------------------ tests ------------------------

    [Fact(DisplayName = "SearchAsync: без предиката -> повертає всіх, відсортовано за ПІБ")]
    public async Task Search_NoPredicate_ReturnsAll_Sorted()
    {
        await SeedAsync(
            P("Бондар", "Анна", "Іванівна", "1111111111"),
            P("Андрух", "Богдан", null, "2222222222"),
            P("Бондар", "Анна", "Андріївна", "3333333333")
        );

        var sut = Svc();

        var res = await sut.SearchAsync(predicate: null);

        Assert.Equal(3, res.Count);
        // Перевіряємо порядок: Андрух..., далі два Бондар...
        Assert.Equal("Андрух", res[0].LastName);
        Assert.Equal("Бондар", res[1].LastName);
        Assert.Equal("Бондар", res[2].LastName);
    }

    [Fact(DisplayName = "SearchAsync: з предикатом -> фільтрує та сортує")]
    public async Task Search_WithPredicate_Filters_And_Sorts()
    {
        await SeedAsync(
        P("Заяць", "Іван", rnokpp: "1234567890"),
        P("Заяць", "Андрій", rnokpp: "2234567890"),
        P("Кіт", "Павло", rnokpp: "3234567890")
    );

        var sut = Svc();

        Expression<Func<Person, bool>> pred = x => x.LastName == "Заяць";
        var res = await sut.SearchAsync(pred);

        // 1) Фільтрація
        Assert.Equal(2, res.Count);
        Assert.All(res, p => Assert.Equal("Заяць", p.LastName));

        // 2) Сортування: перевіряємо, що порядок відповідає ordinal-сорту
        var ord = StringComparer.Ordinal;

        var expectedOrder = res
            .OrderBy(x => x.LastName, ord)
            .ThenBy(x => x.FirstName, ord)
            .ThenBy(x => x.MiddleName, ord)
            .Select(x => x.Id)
            .ToArray();

        var actualOrder = res.Select(x => x.Id).ToArray();

        Assert.Equal(expectedOrder, actualOrder);

        // Додатково – щоб не прив’язуватись до конкретної культурної черги:
        var names = res.Select(x => x.FirstName).ToArray();
        Assert.Contains("Андрій", names);
        Assert.Contains("Іван", names);
    }

    [Fact(DisplayName = "GetByIdAsync: повертає картку з навігаційними полями")]
    public async Task GetById_ReturnsPerson_With_Navigations()
    {
        var unitId = Guid.NewGuid();

        // 1) Спочатку — посада (Code є обов’язковим)
        Ctx.PositionUnits.Add(new PositionUnit
        {
            Id = unitId,
            Code = "INF-001",
            ShortName = "Стрілець",
            OrgPath = "Рота 1"
        });
        await Ctx.SaveChangesAsync();

        // 2) Потім — персона з FK на вже існуючу посаду
        var person = P("Мельник", "Олег", rnokpp: "5555555555");
        person.PositionUnitId = unitId;
        await SeedAsync(person); // SeedAsync ще додасть StatusKind (та інше, якщо потрібно)

        // 3) Перевірка
        var sut = Svc();
        var got = await sut.GetByIdAsync(person.Id);

        Assert.NotNull(got);
        Assert.NotNull(got!.StatusKind);
        Assert.NotNull(got.PositionUnit);
        Assert.Equal("Стрілець", got.PositionUnit!.ShortName);
    }

    [Fact(DisplayName = "CreateAsync: RNOKPP обов'язковий")]
    public async Task Create_Requires_Rnokpp()
    {
        await SeedAsync(); // ensure dicts
        var sut = Svc();
        var p = P("Іванов", "Іван", rnokpp: "");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateAsync(p));
        Assert.Contains("RNOKPP", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "CreateAsync: унікальність RNOKPP перевіряється вручну")]
    public async Task Create_Duplicated_Rnokpp_Throws()
    {
        var exist = P("Іванов", "Іван", rnokpp: "9999999999");
        await SeedAsync(exist);

        var sut = Svc();
        var p = P("Петров", "Петро", rnokpp: "9999999999");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.CreateAsync(p));
        Assert.Contains("вже існує", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "CreateAsync: генерує Id (якщо пустий) і ставить CreatedUtc/ModifiedUtc (якщо поля є)")]
    public async Task Create_Sets_Id_And_Timestamps()
    {
        await SeedAsync();
        var sut = Svc();
        var p = P("Сидоренко", "Олексій", rnokpp: "1010101010");
        p.Id = Guid.Empty;

        var created = await sut.CreateAsync(p);

        Assert.NotEqual(Guid.Empty, created.Id);
        // Якщо у домені додано CreatedUtc/ModifiedUtc — розкоментуй:
        // Assert.NotEqual(default, created.CreatedUtc);
        // Assert.NotEqual(default, created.ModifiedUtc);
        // Assert.True(created.ModifiedUtc >= created.CreatedUtc);
    }

    [Fact(DisplayName = "UpdateAsync: не знайдено -> false")]
    public async Task Update_NotFound_ReturnsFalse()
    {
        await SeedAsync();
        var sut = Svc();

        var p = P("Ghost", "User", rnokpp: "1212121212");
        p.Id = Guid.NewGuid();

        var ok = await sut.UpdateAsync(p);
        Assert.False(ok);
    }

    [Fact(DisplayName = "UpdateAsync: оновлює дозволені поля, не змінює посаду/статус")]
    public async Task Update_UpdatesEditableFields_LeavesPositionAndStatus()
    {
        var unitId = Guid.NewGuid();
        // повний запис посади з обов'язковими полями
        Ctx.PositionUnits.Add(new PositionUnit
        {
            Id = unitId,
            Code = "INF-002",               // ← ДОДАНО
            ShortName = "Стрілець",
            OrgPath = "Рота 1"
        });
        await Ctx.SaveChangesAsync();

        var original = P("Старий", "Ім'я", "ПоБатькові", rnokpp: "1313131313");
        original.PositionUnitId = unitId;
        original.StatusKindId = 1;

        await SeedAsync(original);

        var sut = Svc();

        var patch = new Person
        {
            Id = original.Id,
            LastName = "Новий",
            FirstName = "Петро",
            MiddleName = null,
            Rnokpp = "1414141414",
            Rank = "сержант",
            Callsign = "Новий",
            BZVP = "так",
            Weapon = "АК-74М",

            // «зміни», які сервіс не має чіпати:
            PositionUnitId = Guid.NewGuid(),
            StatusKindId = 999,

            // додаткові поля картки:
            IsAttached = true,
            AttachedFromUnit = "Інший підрозділ"
        };

        var ok = await sut.UpdateAsync(patch);
        Assert.True(ok);

        var got = await Ctx.Persons.AsNoTracking().FirstAsync(p => p.Id == original.Id);

        // оновлені
        Assert.Equal("Новий", got.LastName);
        Assert.Equal("Петро", got.FirstName);
        Assert.Null(got.MiddleName);
        Assert.Equal("1414141414", got.Rnokpp);
        Assert.Equal("сержант", got.Rank);
        Assert.Equal("Новий", got.Callsign);
        Assert.Equal("так", got.BZVP);
        Assert.Equal("АК-74М", got.Weapon);
        Assert.True(got.IsAttached);
        Assert.Equal("Інший підрозділ", got.AttachedFromUnit);

        // НЕ змінені через Update
        Assert.Equal(unitId, got.PositionUnitId);
        Assert.Equal(1, got.StatusKindId);
    }
}
