//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusServiceTests (xUnit)
//-----------------------------------------------------------------------------
//
// Покриті сценарії:
//   1) GetHistoryAsync: повертає відсортовану історію (desc).
//   2) GetActiveAsync: повертає останній відкритий інтервал або null.
//   3) SetStatusAsync: перша установка статусу (створення активного).
//   4) SetStatusAsync: закриття попереднього активного при установці нового.
//   5) SetStatusAsync: заборона моменту всередині закритого інтервалу.
//   6) SetStatusAsync: вимога, щоб новий момент був > за активний.OpenDate.
//   7) SetStatusAsync: повага до правил переходів (IsTransitionAllowedAsync).
//   8) SetStatusAsync: нормалізація дати в UTC (Local/Unspecified).
//   9) Валідаційні помилки: невірні аргументи, відсутні сутності.
//  10) IsTransitionAllowedAsync: null from -> true; allowed/not allowed.
//
// Тестовий провайдер БД: Sqlite In-Memory (реальні транзакції, індекси, тощо)
//
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonStatusService;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using eRaven.Tests.Application.Tests.Helpers;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PersonStatusServiceTests : IDisposable
{
    private readonly SqliteDbHelper _dbh;
    private readonly AppDbContext _db;
    private readonly PersonStatusService _svc;
    private readonly CancellationToken _ct = CancellationToken.None;

    public PersonStatusServiceTests()
    {
        _dbh = new SqliteDbHelper();
        _db = _dbh.Db;
        _svc = new PersonStatusService(_db);
    }

    public void Dispose()
    {
        _dbh.Dispose();
    }

    // ========== helpers ==========

    private static Person NewPerson(string? rnokpp = null) => new()
    {
        Id = Guid.NewGuid(),
        FirstName = "Test",
        LastName = "User",
        MiddleName = null,
        Rnokpp = rnokpp ?? Guid.NewGuid().ToString("N")[..10],
        Rank = "Рядовий",
        CreatedUtc = DateTime.UtcNow,
        ModifiedUtc = DateTime.UtcNow
    };

    private static StatusKind NewKind(string code, string baseName, int order = 0) => new()
    {
        // Id не задаємо — БД призначить
        Code = code,
        Name = $"{baseName}-{Guid.NewGuid():N}", // ← унікальне ім'я, сидінг не зламаємо
        Order = order,
        IsActive = true,
        Modified = DateTime.UtcNow,
        Author = "test"
    };

    private static PersonStatus Ps(Guid personId, int kindId, DateTime openUtc) => new()
    {
        // Id не потрібен, коли додаємо напряму до DbSet — EF згенерує
        PersonId = personId,
        StatusKindId = kindId,
        OpenDate = DateTime.SpecifyKind(openUtc, DateTimeKind.Utc),
        CloseDate = null,
        IsActive = true,
        Note = null,
        Author = "seed",
        Modified = DateTime.UtcNow
    };

    private static StatusTransition Tr(int fromId, int toId) => new()
    {
        FromStatusKindId = fromId,
        ToStatusKindId = toId
    };

    // ========== tests ==========

    [Fact(DisplayName = "GetHistoryAsync: повертає у порядку спадання за OpenDate")]
    public async Task GetHistory_ReturnsDescendingByOpenDate()
    {
        // Arrange
        var p = NewPerson();
        var k1 = NewKind($"A-{Guid.NewGuid():N}", "A");
        var k2 = NewKind($"B-{Guid.NewGuid():N}", "B");
        _db.AddRange(p, k1, k2);
        await _db.SaveChangesAsync(_ct);

        _db.PersonStatuses.AddRange(
            Ps(p.Id, k1.Id, new DateTime(2025, 08, 01, 0, 0, 0, DateTimeKind.Utc)),
            Ps(p.Id, k2.Id, new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc))
        );
        await _db.SaveChangesAsync(_ct);

        // Act
        var list = await _svc.GetHistoryAsync(p.Id, _ct);

        // Assert
        Assert.Equal(2, list.Count);
        Assert.True(list[0].OpenDate > list[1].OpenDate);
    }

    [Fact(DisplayName = "GetActiveAsync: повертає останній відкритий інтервал")]
    public async Task GetActive_ReturnsLatestOpen()
    {
        // Arrange
        var p = NewPerson();
        var k = NewKind($"A-{Guid.NewGuid():N}", "A");
        _db.AddRange(p, k);
        await _db.SaveChangesAsync(_ct);

        // Закритий інтервал
        var closed = Ps(p.Id, k.Id, new DateTime(2025, 08, 01, 0, 0, 0, DateTimeKind.Utc));
        closed.CloseDate = new DateTime(2025, 08, 10, 0, 0, 0, DateTimeKind.Utc);
        closed.IsActive = false;

        // Відкритий інтервал
        var active = Ps(p.Id, k.Id, new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc));

        _db.PersonStatuses.AddRange(closed, active);
        await _db.SaveChangesAsync(_ct);

        // Act
        var got = await _svc.GetActiveAsync(p.Id, _ct);

        // Assert
        Assert.NotNull(got);
        Assert.Null(got!.CloseDate);
        Assert.Equal(active.OpenDate, got.OpenDate);
    }

    [Fact(DisplayName = "SetStatusAsync: перша установка створює активний інтервал та оновлює Person.StatusKindId")]
    public async Task SetStatus_FirstInstall_CreatesActiveAndUpdatesPerson()
    {
        // Arrange
        var p = NewPerson();
        var k = NewKind($"IN_{Guid.NewGuid():N}", "В районі");
        _db.AddRange(p, k);
        await _db.SaveChangesAsync(_ct);

        var toSet = new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k.Id,
            OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act
        var saved = await _svc.SetStatusAsync(toSet, _ct);

        // Assert
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.True(saved.IsActive);
        Assert.Equal(k.Id, saved.StatusKindId);

        var person = await _db.Persons.FindAsync([p.Id], _ct);
        Assert.Equal(k.Id, person!.StatusKindId);

        var active = await _svc.GetActiveAsync(p.Id, _ct);
        Assert.NotNull(active);
        Assert.Equal(saved.Id, active!.Id);
    }

    [Fact(DisplayName = "SetStatusAsync: закриває попередній активний інтервал і відкриває новий")]
    public async Task SetStatus_ClosesPreviousActive()
    {
        // Arrange
        var p = NewPerson();
        var k1 = NewKind($"K1_{Guid.NewGuid():N}", "K1");
        var k2 = NewKind($"K2_{Guid.NewGuid():N}", "K2");
        _db.AddRange(p, k1, k2);
        await _db.SaveChangesAsync(_ct);

        // Дозволити перехід k1 -> k2
        _db.Add(Tr(k1.Id, k2.Id));
        await _db.SaveChangesAsync(_ct);

        var active = Ps(p.Id, k1.Id, new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc));
        _db.PersonStatuses.Add(active);
        await _db.SaveChangesAsync(_ct);

        var newMoment = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc);
        var toSet = new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k2.Id,
            OpenDate = newMoment
        };

        // Act
        var saved = await _svc.SetStatusAsync(toSet, _ct);

        // Assert
        await _db.Entry(active).ReloadAsync(_ct);
        Assert.True(active.IsActive);
        Assert.Equal(newMoment, active.CloseDate);

        Assert.True(saved.IsActive);
        Assert.Equal(k2.Id, saved.StatusKindId);
    }

    [Theory(DisplayName = "SetStatusAsync: відхиляє момент, який не пізніший за відкритий активний")]
    [InlineData(2025, 9, 10, 2025, 9, 9)]  // раніше
    [InlineData(2025, 9, 10, 2025, 9, 10)] // рівно
    public async Task SetStatus_Fails_IfNotLaterThanActive(
        int ay, int am, int ad, int ny, int nm, int nd)
    {
        // Arrange
        var p = NewPerson();
        var k1 = NewKind($"K1_{Guid.NewGuid():N}", "K1");
        var k2 = NewKind($"K2_{Guid.NewGuid():N}", "K2");
        _db.AddRange(p, k1, k2);
        await _db.SaveChangesAsync(_ct);

        _db.Add(Tr(k1.Id, k2.Id));
        await _db.SaveChangesAsync(_ct);

        var activeMoment = new DateTime(ay, am, ad, 0, 0, 0, DateTimeKind.Utc);
        _db.PersonStatuses.Add(Ps(p.Id, k1.Id, activeMoment));
        await _db.SaveChangesAsync(_ct);

        var newMoment = new DateTime(ny, nm, nd, 0, 0, 0, DateTimeKind.Utc);

        var toSet = new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k2.Id,
            OpenDate = newMoment
        };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.SetStatusAsync(toSet, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: відхиляє момент усередині закритого інтервалу")]
    public async Task SetStatus_Fails_IfInsideClosedInterval()
    {
        // Arrange
        var p = NewPerson();
        var k = NewKind($"K_{Guid.NewGuid():N}", "K");
        _db.AddRange(p, k);
        await _db.SaveChangesAsync(_ct);

        // Закритий інтервал [t1..t2]
        var t1 = new DateTime(2025, 08, 01, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2025, 08, 10, 0, 0, 0, DateTimeKind.Utc);
        var closed = Ps(p.Id, k.Id, t1);
        closed.CloseDate = t2;
        closed.IsActive = false;
        _db.PersonStatuses.Add(closed);
        await _db.SaveChangesAsync(_ct);

        // Проба стати всередину
        var toSet = new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k.Id,
            OpenDate = new DateTime(2025, 08, 05, 0, 0, 0, DateTimeKind.Utc)
        };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.SetStatusAsync(toSet, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: відхиляє неіснуючий StatusKind")]
    public async Task SetStatus_Fails_IfStatusKindMissing()
    {
        // Arrange
        var p = NewPerson();
        _db.Add(p);
        await _db.SaveChangesAsync(_ct);

        var toSet = new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = int.MaxValue, // гарантовано не існує
            OpenDate = DateTime.UtcNow
        };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _svc.SetStatusAsync(toSet, _ct));
    }

    [Fact(DisplayName = "IsTransitionAllowedAsync: true для першої установки, працює за правилом (from->to)")]
    public async Task IsTransitionAllowed_Works()
    {
        // Arrange
        var a = NewKind($"A_{Guid.NewGuid():N}", "A");
        var b = NewKind($"B_{Guid.NewGuid():N}", "B");
        _db.AddRange(a, b);
        await _db.SaveChangesAsync(_ct);

        _db.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        // Act + Assert
        Assert.True(await _svc.IsTransitionAllowedAsync(null, b.Id, _ct));    // перша установка
        Assert.True(await _svc.IsTransitionAllowedAsync(a.Id, b.Id, _ct));    // дозволена
        Assert.False(await _svc.IsTransitionAllowedAsync(b.Id, a.Id, _ct));   // зворотна — заборонена
    }

    [Fact(DisplayName = "SetStatusAsync: нормалізує OpenDate до UTC (Unspecified/Local)")]
    public async Task SetStatus_NormalizesOpenDateToUtc()
    {
        // Arrange
        var p = NewPerson();
        var k1 = NewKind($"K_{Guid.NewGuid():N}", "K");    // перший статус
        var k2 = NewKind($"K_{Guid.NewGuid():N}", "K2");   // другий статус (інший!)
        _db.AddRange(p, k1, k2);
        await _db.SaveChangesAsync(_ct);

        // Unspecified → стає Utc (ті ж ticks, лише Kind=Utc)
        var unspecified = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Unspecified);

        var saved1 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k1.Id,
            OpenDate = unspecified
        }, _ct);

        Assert.Equal(DateTimeKind.Utc, saved1.OpenDate.Kind);
        Assert.Equal(unspecified.Ticks, saved1.OpenDate.Ticks); // SpecifyKind-поведінка

        // Дозволяємо перехід k1 -> k2 (самоперехід k1->k1 заборонено CHECK-ом у БД)
        _db.StatusTransitions.Add(Tr(k1.Id, k2.Id));
        await _db.SaveChangesAsync(_ct);

        // Local → ToUniversalTime()
        // Щоб уникнути флаків через часові пояси CI-середовища,
        // беремо дату помітно пізніше, ніж попередня.
        var local = new DateTime(2026, 01, 01, 0, 0, 0, DateTimeKind.Local);

        var saved2 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k2.Id,
            OpenDate = local
        }, _ct);

        Assert.Equal(DateTimeKind.Utc, saved2.OpenDate.Kind);
        Assert.Equal(local.ToUniversalTime(), saved2.OpenDate);
    }
}