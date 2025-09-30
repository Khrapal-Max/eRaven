//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonServiceTests
//-----------------------------------------------------------------------------

using System;
using System.Threading;
using eRaven.Application.Services.PersonService;
using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using eRaven.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PersonServiceTests : IDisposable
{
    private readonly SqliteDbHelper _helper;
    private readonly FakeClock _clock;
    private readonly PersonService _svc;

    public PersonServiceTests()
    {
        _helper = new SqliteDbHelper();
        _clock = new FakeClock(new DateTime(2030, 01, 01, 0, 0, 0, DateTimeKind.Utc));
        _svc = new PersonService(_helper.Factory, _clock);
    }

    public void Dispose()
    {
        _helper.Dispose();
        GC.SuppressFinalize(this);
    }

    // ------------------------ helpers ------------------------

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

    /// <summary>Сидування базових довідників + осіб (за потреби) в окремому контексті.</summary>
    private async Task SeedAsync(params Person[] people)
    {
        await using var db = _helper.CreateContext();

        if (!await db.StatusKinds.AnyAsync())
            db.StatusKinds.Add(new StatusKind
            {
                Id = 1,
                Name = "В районі",
                Code = "RDY",
                Order = 1,
                IsActive = true
            });

        if (!await db.PositionUnits.AnyAsync())
            db.PositionUnits.Add(new PositionUnit
            {
                Id = Guid.NewGuid(),
                Code = "VAC-000",
                ShortName = "Вакант",
                OrgPath = "—",
                SpecialNumber = "000",
                IsActived = true
            });

        if (people is { Length: > 0 })
            db.Persons.AddRange(people);

        await db.SaveChangesAsync(CancellationToken.None);
    }

    // ------------------------ tests ------------------------

    [Fact(DisplayName = "SearchAsync: без предиката -> повертає всіх, відсортовано за ПІБ")]
    public async Task Search_NoPredicate_ReturnsAll_Sorted()
    {
        await SeedAsync(
            P("Бондар", "Анна", "Іванівна", "1111111111"),
            P("Андрух", "Богдан", null, "2222222222"),
            P("Бондар", "Анна", "Андріївна", "3333333333")
        );

        var res = await _svc.SearchAsync(predicate: null);

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

        Expression<Func<Person, bool>> pred = x => x.LastName == "Заяць";
        var res = await _svc.SearchAsync(pred);

        // 1) Фільтрація
        Assert.Equal(2, res.Count);
        Assert.All(res, p => Assert.Equal("Заяць", p.LastName));

        // 2) Сортування (ordinal)
        var ord = StringComparer.Ordinal;
        var expectedOrder = res
            .OrderBy(x => x.LastName, ord)
            .ThenBy(x => x.FirstName, ord)
            .ThenBy(x => x.MiddleName, ord)
            .Select(x => x.Id)
            .ToArray();
        var actualOrder = res.Select(x => x.Id).ToArray();

        Assert.Equal(expectedOrder, actualOrder);

        var names = res.Select(x => x.FirstName).ToArray();
        Assert.Contains("Андрій", names);
        Assert.Contains("Іван", names);
    }

    [Fact(DisplayName = "GetByIdAsync: повертає картку з навігаційними полями")]
    public async Task GetById_ReturnsPerson_With_Navigations()
    {
        var unitId = Guid.NewGuid();

        // 1) Посада
        await using (var db = _helper.CreateContext())
        {
            db.PositionUnits.Add(new PositionUnit
            {
                Id = unitId,
                Code = "INF-001",
                ShortName = "Стрілець",
                OrgPath = "Рота 1",
                SpecialNumber = "123",
                IsActived = true
            });
            await db.SaveChangesAsync(CancellationToken.None);
        }

        // 2) Персона з FK
        var person = P("Мельник", "Олег", rnokpp: "5555555555");
        person.PositionUnitId = unitId;
        await SeedAsync(person);

        // 3) Перевірка
        var got = await _svc.GetByIdAsync(person.Id);

        Assert.NotNull(got);
        Assert.NotNull(got!.StatusKind);
        Assert.NotNull(got.PositionUnit);
        Assert.Equal("Стрілець", got.PositionUnit!.ShortName);
    }

    [Fact(DisplayName = "CreateAsync: RNOKPP обов'язковий")]
    public async Task Create_Requires_Rnokpp()
    {
        await SeedAsync(); // ensure 

        var p = P("Іванов", "Іван", rnokpp: "");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _svc.CreateAsync(p));
        Assert.Contains("RNOKPP", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "CreateAsync: унікальність RNOKPP перевіряється вручну")]
    public async Task Create_Duplicated_Rnokpp_Throws()
    {
        var exist = P("Іванов", "Іван", rnokpp: "9999999999");
        await SeedAsync(exist);

        var p = P("Петров", "Петро", rnokpp: "9999999999");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.CreateAsync(p));
        Assert.Contains("вже існує", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact(DisplayName = "CreateAsync: генерує Id (якщо пустий) і ставить CreatedUtc/ModifiedUtc (якщо поля є)")]
    public async Task Create_Sets_Id_And_Timestamps()
    {
        await SeedAsync();

        var p = P("Сидоренко", "Олексій", rnokpp: "1010101010");
        p.Id = Guid.Empty;

        _clock.UtcNow = new DateTime(2030, 02, 01, 12, 0, 0, DateTimeKind.Utc);
        var created = await _svc.CreateAsync(p);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(_clock.UtcNow, created.CreatedUtc);
        Assert.Equal(_clock.UtcNow, created.ModifiedUtc);
    }

    [Fact(DisplayName = "UpdateAsync: не знайдено -> false")]
    public async Task Update_NotFound_ReturnsFalse()
    {
        await SeedAsync();

        var p = P("Ghost", "User", rnokpp: "1212121212");
        p.Id = Guid.NewGuid();

        var ok = await _svc.UpdateAsync(p);
        Assert.False(ok);
    }

    [Fact(DisplayName = "UpdateAsync: оновлює дозволені поля, не змінює посаду/статус")]
    public async Task Update_UpdatesEditableFields_LeavesPositionAndStatus()
    {
        var unitId = Guid.NewGuid();

        // повний запис посади з обов'язковими полями
        await using (var db = _helper.CreateContext())
        {
            db.PositionUnits.Add(new PositionUnit
            {
                Id = unitId,
                Code = "INF-002",
                ShortName = "Стрілець",
                OrgPath = "Рота 1",
                SpecialNumber = "456",
                IsActived = true
            });
            await db.SaveChangesAsync(CancellationToken.None);
        }

        var original = P("Старий", "Ім'я", "ПоБатькові", rnokpp: "1313131313");
        original.PositionUnitId = unitId;
        original.StatusKindId = 1;

        await SeedAsync(original);

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

        _clock.UtcNow = new DateTime(2030, 03, 01, 9, 0, 0, DateTimeKind.Utc);
        var expectedModified = _clock.UtcNow;

        var ok = await _svc.UpdateAsync(patch);
        Assert.True(ok);

        await using var read = _helper.CreateContext();
        var got = await read.Persons.AsNoTracking().FirstAsync(p => p.Id == original.Id);

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
        Assert.Equal(expectedModified, got.ModifiedUtc);
    }
}
