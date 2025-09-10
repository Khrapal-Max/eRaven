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

    // ---------- helpers ----------

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

    private static StatusKind NewKind(string code, string name) => new()
    {
        Id = 0,
        Code = code,
        Name = name,
        IsActive = true,
        Author = "test",
        Modified = DateTime.UtcNow
    };

    private static StatusKind NewKindUnique(string? namePrefix = null)
    => new()
    {
        Id = 0, // авто
        Code = $"K_{Guid.NewGuid():N}",                         // унікальний code
        Name = $"{namePrefix ?? "Kind"}_{Guid.NewGuid():N}",    // унікальний name
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

    // ---------- tests ----------

    [Fact(DisplayName = "SetStatusAsync: перша установка створює валідний запис, Person.StatusKindId оновлюється")]
    public async Task SetStatus_FirstInstall_CreatesValid_UpdatesPerson()
    {
        var person = NewPerson();
        var kind = NewKindUnique(); // ⬅️ унікальні Code/Name
        _db.AddRange(person, kind);
        await _db.SaveChangesAsync(_ct);

        var saved = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = kind.Id,
            OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc)
        }, _ct);

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
        var person = NewPerson();
        var a = NewKind("A", "A");
        var b = NewKind("B", "B");
        _db.AddRange(person, a, b);
        await _db.SaveChangesAsync(_ct);

        // 1) Unspecified → Utc (SpecifyKind)
        var unspecified = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Unspecified);
        var s1 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = a.Id,
            OpenDate = unspecified
        }, _ct);

        Assert.Equal(DateTimeKind.Utc, s1.OpenDate.Kind);
        Assert.Equal(unspecified.Ticks, s1.OpenDate.Ticks);

        // Дозволяємо A → B
        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        // 2) Local → Utc, інший вид статусу
        var local = new DateTime(2025, 09, 02, 0, 0, 0, DateTimeKind.Local);
        var s2 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = b.Id,
            OpenDate = local
        }, _ct);

        Assert.Equal(DateTimeKind.Utc, s2.OpenDate.Kind);
        Assert.Equal(local.ToUniversalTime(), s2.OpenDate);
    }

    [Fact(DisplayName = "SetStatusAsync: відхиляє момент ≤ останнього валідного")]
    public async Task SetStatus_Rejects_NonIncreasingMoment()
    {
        var person = NewPerson();
        var a = NewKind("A", "A");
        var b = NewKind("B", "B");
        _db.AddRange(person, a, b);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);
        var t0 = t1.AddHours(-1);

        _ = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = a.Id,
            OpenDate = t1
        }, _ct);

        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        // минуле
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = person.Id,
                StatusKindId = b.Id,
                OpenDate = t0
            }, _ct));

        // той самий момент
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = person.Id,
                StatusKindId = b.Id,
                OpenDate = t1
            }, _ct));
    }

    [Fact(DisplayName = "UpdateStateIsActive: деактивація поточного оновлює Person.StatusKindId на попередній валідний")]
    public async Task UpdateStateIsActive_Deactivate_UpdatesPersonToPrevious()
    {
        var person = NewPerson();
        var a = NewKind("A", "A");
        var b = NewKind("B", "B");
        _db.AddRange(person, a, b);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);
        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = a.Id, OpenDate = t1 }, _ct);

        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        var t2 = t1.AddHours(1);
        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = b.Id, OpenDate = t2 }, _ct);

        var nowInactive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.False(nowInactive);

        var reloadedPerson = await _db.Persons.FindAsync([person.Id], _ct);
        Assert.Equal(a.Id, reloadedPerson!.StatusKindId);
    }

    [Fact(DisplayName = "UpdateStateIsActive: активація при конфлікті key(person,open,seq) піднімає Sequence")]
    public async Task UpdateStateIsActive_Activate_ResolvesUniqueConflict_BySequenceBump()
    {
        var person = NewPerson();
        var a = NewKind("A", "A");
        var b = NewKind("B", "B");
        _db.AddRange(person, a, b);
        await _db.SaveChangesAsync(_ct);

        var t = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);

        // Базовий активний запис s1 @ t, seq=0
        var s1 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = a.Id,
            OpenDate = t
        }, _ct);

        // Додаємо пізніший валідний запис s2 (інший вид статусу)
        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        var s2 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = person.Id,
            StatusKindId = b.Id,
            OpenDate = t.AddHours(1)
        }, _ct);

        // Деактивуємо s2
        var nowInactive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.False(nowInactive);

        // Імітуємо "історичний дубль": виставляємо s2 на той самий ключ (person, open, seq) що й s1
        s2.OpenDate = s1.OpenDate;
        s2.Sequence = s1.Sequence; // 0
        await _db.SaveChangesAsync(_ct);

        // Реактивуємо → сервіс має підняти sequence, щоб уникнути конфлікту з активним s1
        var nowActive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.True(nowActive);

        var reloaded = await _db.PersonStatuses.FirstAsync(x => x.Id == s2.Id, _ct);
        Assert.Equal((short)1, reloaded.Sequence); // піднято до наступного доступного
    }
}
