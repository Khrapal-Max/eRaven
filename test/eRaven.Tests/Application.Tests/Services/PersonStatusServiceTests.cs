//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusServiceTests (Sequence + no CloseDate)
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PersonStatusService;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PersonStatusServiceTests : IDisposable
{
    private readonly SqliteDbHelper _helper = new();
    private readonly AppDbContext _db;
    private readonly IPersonStatusService _svc;
    private readonly CancellationToken _ct = default;

    public PersonStatusServiceTests()
    {
        _db = _helper.Db;
        _svc = new PersonStatusService(_db);
    }

    public void Dispose() => _helper.Dispose();

    // ---------- Helpers ----------

    private static Person NewPerson() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Іванов",
        FirstName = "Іван",
        MiddleName = "Іванович",
        Rnokpp = "1111111111",
        CreatedUtc = DateTime.UtcNow,
        ModifiedUtc = DateTime.UtcNow
    };

    private static StatusKind NewKindUnique(string? namePrefix = null) => new()
    {
        Id = 0, // авто
        Code = $"K_{Guid.NewGuid():N}",                       // унікальний code
        Name = $"{namePrefix ?? "Kind"}_{Guid.NewGuid():N}",  // унікальний name (з префіксом для читабельності)
        IsActive = true,
        Author = "test",
        Modified = DateTime.UtcNow
    };

    private static StatusTransition Tr(int fromId, int toId) => new()
    {
        Id = 0,
        FromStatusKindId = fromId,
        ToStatusKindId = toId
    };

    // ---------- Tests ----------

    [Fact(DisplayName = "SetStatusAsync: перша установка створює валідний запис, Person.StatusKindId оновлюється")]
    public async Task SetStatus_FirstInstall_CreatesValid_UpdatesPerson()
    {
        // Arrange
        var person = NewPerson();
        var kind = NewKindUnique("Start");
        _db.AddRange(person, kind);
        await _db.SaveChangesAsync(_ct);

        // Act
        var saved = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kind.Id,
            OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc)
        }, _ct);

        // Assert
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.True(saved.IsActive);
        Assert.Equal((short)0, saved.Sequence);
        Assert.Equal(kind.Id, saved.StatusKindId);

        var reloadedPerson = await _db.Persons.FindAsync([person.Id], _ct);
        Assert.Equal(kind.Id, reloadedPerson!.StatusKindId);

        var active = await _svc.GetActiveAsync(person.Id, _ct);
        Assert.NotNull(active);
        Assert.Equal(saved.Id, active!.Id);
    }

    [Fact(DisplayName = "SetStatusAsync: нормалізує OpenDate до UTC (Unspecified/Local) і проходить A→B")]
    public async Task SetStatus_NormalizesOpenDateToUtc()
    {
        // Arrange
        var person = NewPerson();
        var kindA = NewKindUnique("A");
        var kindB = NewKindUnique("B");
        _db.AddRange(person, kindA, kindB);
        await _db.SaveChangesAsync(_ct);

        // 1) Unspecified → Utc (SpecifyKind)
        var unspecified = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Unspecified);

        // Act
        var s1 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kindA.Id,
            OpenDate = unspecified
        }, _ct);

        // Assert
        Assert.Equal(DateTimeKind.Utc, s1.OpenDate.Kind);
        Assert.Equal(unspecified.Ticks, s1.OpenDate.Ticks);

        // 2) Дозволяємо A → B
        _db.StatusTransitions.Add(Tr(kindA.Id, kindB.Id));
        await _db.SaveChangesAsync(_ct);

        // 3) Local → Utc, інший вид
        var local = new DateTime(2025, 09, 02, 0, 0, 0, DateTimeKind.Local);

        var s2 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kindB.Id,
            OpenDate = local
        }, _ct);

        Assert.Equal(DateTimeKind.Utc, s2.OpenDate.Kind);
        Assert.Equal(local.ToUniversalTime(), s2.OpenDate);
    }

    [Fact(DisplayName = "SetStatusAsync: відхиляє момент ≤ останнього валідного")]
    public async Task SetStatus_Rejects_NonIncreasingMoment()
    {
        // Arrange
        var person = NewPerson();
        var kindA = NewKindUnique("A");
        var kindB = NewKindUnique("B");
        _db.AddRange(person, kindA, kindB);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);
        var t0 = t1.AddHours(-1);

        _ = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kindA.Id,
            OpenDate = t1
        }, _ct);

        _db.StatusTransitions.Add(Tr(kindA.Id, kindB.Id));
        await _db.SaveChangesAsync(_ct);

        // Act + Assert: минуле
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = person.Id,
                StatusKindId = kindB.Id,
                OpenDate = t0
            }, _ct));

        // Act + Assert: той самий момент
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = person.Id,
                StatusKindId = kindB.Id,
                OpenDate = t1
            }, _ct));
    }

    [Fact(DisplayName = "UpdateStateIsActive: деактивація поточного оновлює Person.StatusKindId на попередній валідний")]
    public async Task UpdateStateIsActive_Deactivate_UpdatesPersonToPrevious()
    {
        // Arrange
        var person = NewPerson();
        var kindA = NewKindUnique("A");
        var kindB = NewKindUnique("B");
        _db.AddRange(person, kindA, kindB);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);
        var s1 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kindA.Id,
            OpenDate = t1
        }, _ct);

        _db.StatusTransitions.Add(Tr(kindA.Id, kindB.Id));
        await _db.SaveChangesAsync(_ct);

        var t2 = t1.AddHours(1);
        var s2 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kindB.Id,
            OpenDate = t2
        }, _ct);

        // Act
        var nowInactive = await _svc.UpdateStateIsActive(s2.Id, _ct);

        // Assert
        Assert.False(nowInactive);
        var reloadedPerson = await _db.Persons.FindAsync([person.Id], _ct);
        Assert.Equal(kindA.Id, reloadedPerson!.StatusKindId);
    }

    [Fact(DisplayName = "UpdateStateIsActive: активація при конфлікті key(person,open,seq) піднімає Sequence")]
    public async Task UpdateStateIsActive_Activate_ResolvesUniqueConflict_BySequenceBump()
    {
        // Arrange
        var person = NewPerson();
        var kindA = NewKindUnique("A");
        var kindB = NewKindUnique("B");
        _db.AddRange(person, kindA, kindB);
        await _db.SaveChangesAsync(_ct);

        var t = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);

        // Базовий активний запис s1 @ t, seq=0
        var s1 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kindA.Id,
            OpenDate = t
        }, _ct);

        // Додаємо пізніший валідний запис s2
        _db.StatusTransitions.Add(Tr(kindA.Id, kindB.Id));
        await _db.SaveChangesAsync(_ct);

        var s2 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kindB.Id,
            OpenDate = t.AddHours(1)
        }, _ct);

        // Деактивуємо s2
        var nowInactive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.False(nowInactive);

        // Імітуємо «історичний дубль»: зробимо той самий ключ, що у s1 (person, open, seq)
        s2.OpenDate = s1.OpenDate;
        s2.Sequence = s1.Sequence; // 0
        await _db.SaveChangesAsync(_ct);

        // Act: реактивація → сервіс має підняти sequence, щоб уникнути конфлікту
        var nowActive = await _svc.UpdateStateIsActive(s2.Id, _ct);

        // Assert
        Assert.True(nowActive);

        var reloaded = await _db.PersonStatuses.FirstAsync(x => x.Id == s2.Id, _ct);
        Assert.Equal((short)1, reloaded.Sequence); // піднято до наступного доступного
    }
}
