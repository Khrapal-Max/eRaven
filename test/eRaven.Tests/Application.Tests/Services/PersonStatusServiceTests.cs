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
    private readonly IPersonStatusService _personStatusService;
    private readonly CancellationToken _ct = default;

    public PersonStatusServiceTests()
    {
        _db = _helper.Db;
        _personStatusService = new PersonStatusService(_db);
    }

    public void Dispose() => _helper.Dispose();

    // -------- Helpers --------

    private static Person NewPerson()
        => new()
        {
            Id = Guid.NewGuid(),
            LastName = "Іванов",
            FirstName = "Іван",
            MiddleName = "Іванович",
            Rnokpp = "1111111111",
            CreatedUtc = DateTime.UtcNow,
            ModifiedUtc = DateTime.UtcNow
        };

    private static StatusKind NewKind(string code, string name)
        => new()
        {
            Id = 0, // авто
            Code = code,
            Name = name,
            IsActive = true,
            Author = "test",
            Modified = DateTime.UtcNow
        };

    private static StatusTransition Tr(int fromId, int toId)
        => new()
        {
            Id = 0,
            FromStatusKindId = fromId,
            ToStatusKindId = toId
        };

    // -------- Tests --------

    [Fact(DisplayName = "SetStatusAsync: перша установка створює валідний запис, Person.StatusKindId оновлюється")]
    public async Task SetStatus_FirstInstall_CreatesValid_UpdatesPerson()
    {
        var p = NewPerson();
        var k = NewKind("30", "В районі");
        _db.AddRange(p, k);
        await _db.SaveChangesAsync(_ct);

        var saved = await _personStatusService.SetStatusAsync(new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k.Id,
            OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc)
        }, _ct);

        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.True(saved.IsActive);
        Assert.Equal((short)0, saved.Sequence); // перший на момент → seq=0
        Assert.Equal(k.Id, saved.StatusKindId);

        var person = await _db.Persons.FindAsync([p.Id], _ct);
        Assert.Equal(k.Id, person!.StatusKindId);

        var active = await _personStatusService.GetActiveAsync(p.Id, _ct);
        Assert.NotNull(active);
        Assert.Equal(saved.Id, active!.Id);
    }

    [Fact(DisplayName = "SetStatusAsync: нормалізує OpenDate до UTC (Unspecified/Local)")]
    public async Task SetStatus_NormalizesOpenDateToUtc()
    {
        var p = NewPerson();
        var k = NewKind("A", "A");
        _db.AddRange(p, k);
        await _db.SaveChangesAsync(_ct);

        var unspecified = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Unspecified);
        var saved1 = await _personStatusService.SetStatusAsync(new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k.Id,
            OpenDate = unspecified
        }, _ct);

        Assert.Equal(DateTimeKind.Utc, saved1.OpenDate.Kind);
        Assert.Equal(unspecified.Ticks, saved1.OpenDate.Ticks); // SpecifyKind semantics

        // дозволь перехід A->A
        _db.StatusTransitions.Add(Tr(k.Id, k.Id));
        await _db.SaveChangesAsync(_ct);

        var local = new DateTime(2025, 09, 02, 0, 0, 0, DateTimeKind.Local);
        var saved2 = await _personStatusService.SetStatusAsync(new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = k.Id,
            OpenDate = local
        }, _ct);

        Assert.Equal(DateTimeKind.Utc, saved2.OpenDate.Kind);
        Assert.Equal(local.ToUniversalTime(), saved2.OpenDate);
    }

    [Fact(DisplayName = "SetStatusAsync: на той самий момент часу збільшує Sequence (0,1,2,...)")]
    public async Task SetStatus_IncrementsSequence_OnSameMoment()
    {
        var p = NewPerson();
        var k = NewKind("X", "X");
        _db.AddRange(p, k);
        await _db.SaveChangesAsync(_ct);

        var t = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);

        // 1-й запис
        var s1 = await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = k.Id, OpenDate = t }, _ct);
        Assert.Equal((short)0, s1.Sequence);

        // дозволяємо X->X
        _db.StatusTransitions.Add(Tr(k.Id, k.Id));
        await _db.SaveChangesAsync(_ct);

        // 2-й на той самий момент
        var s2 = await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = k.Id, OpenDate = t }, _ct);
        Assert.Equal((short)1, s2.Sequence);

        var active = await _personStatusService.GetActiveAsync(p.Id, _ct);
        Assert.Equal(s2.Id, active!.Id); // останній по (OpenDate, Sequence)
    }

    [Fact(DisplayName = "SetStatusAsync: відхиляє момент <= останнього валідного")]
    public async Task SetStatus_Rejects_NonIncreasingMoment()
    {
        var p = NewPerson();
        var a = NewKind("A", "A");
        var b = NewKind("B", "B");
        _db.AddRange(p, a, b);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);
        var t0 = t1.AddHours(-1);

        // перший
        _ = await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = a.Id, OpenDate = t1 }, _ct);

        // правило A->B
        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        // спроба поставити в минуле/той самий момент — має впасти
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = b.Id, OpenDate = t0 }, _ct));

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = b.Id, OpenDate = t1 }, _ct));
    }

    [Fact(DisplayName = "UpdateStateIsActive: деактивація поточного оновлює Person.StatusKindId на попередній валідний")]
    public async Task UpdateStateIsActive_Deactivate_UpdatesPersonToPrevious()
    {
        var p = NewPerson();
        var a = NewKind("A", "A");
        var b = NewKind("B", "B");
        _db.AddRange(p, a, b);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);
        var s1 = await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = a.Id, OpenDate = t1 }, _ct);

        // правило A->B
        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        var t2 = t1.AddHours(1);
        var s2 = await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = b.Id, OpenDate = t2 }, _ct);

        // деактивуємо поточний (s2)
        var newVal = await _personStatusService.UpdateStateIsActive(s2.Id, _ct);
        Assert.False(newVal);

        var person = await _db.Persons.FindAsync([p.Id], _ct);
        Assert.Equal(a.Id, person!.StatusKindId); // повернувся на попередній валідний
    }

    [Fact(DisplayName = "UpdateStateIsActive: активація при конфлікті key(person,open,seq) підвищує Sequence")]
    public async Task UpdateStateIsActive_Activate_ResolvesUniqueConflict_BySequenceBump()
    {
        var p = NewPerson();
        var a = NewKind("A", "A");
        _db.AddRange(p, a);
        await _db.SaveChangesAsync(_ct);

        var t = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);

        var s1 = await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = a.Id, OpenDate = t }, _ct);
        // дозволь A->A і додай ще один на той самий момент
        _db.StatusTransitions.Add(Tr(a.Id, a.Id));
        await _db.SaveChangesAsync(_ct);

        var s2 = await _personStatusService.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = a.Id, OpenDate = t }, _ct);
        Assert.Equal(1, s2.Sequence);

        // деактивуємо s2
        var nowInactive = await _personStatusService.UpdateStateIsActive(s2.Id, _ct);
        Assert.False(nowInactive);

        // вручну встановимо s2.Sequence = 0 (імітуємо історичний дубль),
        // і спробуємо знову активувати → сервіс має підняти sequence до 2
        s2.Sequence = 0;
        await _db.SaveChangesAsync(_ct);

        var nowActive = await _personStatusService.UpdateStateIsActive(s2.Id, _ct);
        Assert.True(nowActive);

        var reloaded = await _db.PersonStatuses.FirstAsync(x => x.Id == s2.Id, _ct);
        Assert.Equal(2, reloaded.Sequence); // піднято, щоб не конфліктувати з s1(0) і попереднім s2(1)
    }
}
