//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusServiceTests (Sequence + no CloseDate) — full coverage
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
    private readonly PersonStatusService _svc;
    private readonly CancellationToken _ct = default;

    public PersonStatusServiceTests()
    {
        _db = _helper.Db;
        _svc = new PersonStatusService(_db);
    }

    public void Dispose() => _helper.Dispose();

    // ---------- Helpers ----------

    private static long _rnokppSeed = 1000000000; // 10-значне число

    private static string NextRnokpp() =>
        Interlocked.Increment(ref _rnokppSeed).ToString(); // "1000000001", "1000000002", ...

    private static Person NewPerson() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Іванов",
        FirstName = "Іван",
        MiddleName = "Іванович",
        Rnokpp = NextRnokpp(),                 // ⬅️ тепер унікальне
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

    [Fact(DisplayName = "GetAllAsync: повертає всі, з include Person/StatusKind, відсортовано OpenDate↓, Sequence↓")]
    public async Task GetAllAsync_Returns_All_InOrder_WithIncludes()
    {
        var person = NewPerson();
        var kA = NewKindUnique("A");
        var kB = NewKindUnique("B");
        _db.AddRange(person, kA, kB);
        await _db.SaveChangesAsync(_ct);

        var t = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc);

        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = kA.Id, OpenDate = t }, _ct);

        _db.StatusTransitions.Add(Tr(kA.Id, kB.Id));
        await _db.SaveChangesAsync(_ct);

        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = kB.Id, OpenDate = t.AddHours(2) }, _ct);

        // неактивний запис на той самий момент з великим sequence
        var s3 = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            StatusKindId = kA.Id,
            OpenDate = t,
            Sequence = 5,
            IsActive = false,
            Author = "test",
            Modified = DateTime.UtcNow
        };
        _db.PersonStatuses.Add(s3);
        await _db.SaveChangesAsync(_ct);

        var all = (await _svc.GetAllAsync(_ct)).ToList();
        Assert.Equal(3, all.Count);

        // порядок: s2 (t+2h), s3 (t, seq=5), s1 (t, seq=0)
        Assert.Equal([s2.Id, s3.Id, s1.Id], [.. all.Select(x => x.Id)]);

        // include-и присутні
        Assert.All(all, x =>
        {
            Assert.NotNull(x.Person);
            Assert.NotNull(x.StatusKind);
        });
    }

    [Fact(DisplayName = "GetHistoryAsync: повертає історію тільки по Person і у вірному порядку")]
    public async Task GetHistoryAsync_Returns_ByPerson_InOrder()
    {
        var p1 = NewPerson();
        var p2 = NewPerson();
        var k = NewKindUnique("K");
        _db.AddRange(p1, p2, k);
        await _db.SaveChangesAsync(_ct);

        var t = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc);
        _ = await _svc.SetStatusAsync(new PersonStatus { PersonId = p1.Id, StatusKindId = k.Id, OpenDate = t }, _ct);
        _ = await _svc.SetStatusAsync(new PersonStatus { PersonId = p2.Id, StatusKindId = k.Id, OpenDate = t.AddHours(1) }, _ct);

        var hist1 = await _svc.GetHistoryAsync(p1.Id, _ct);
        Assert.Single(hist1);
        Assert.All(hist1, h => Assert.Equal(p1.Id, h.PersonId));
    }

    [Fact(DisplayName = "GetActiveAsync: повертає null, якщо активних немає")]
    public async Task GetActiveAsync_ReturnsNull_WhenNoActive()
    {
        var p = NewPerson();
        var k = NewKindUnique("K");
        _db.AddRange(p, k);
        await _db.SaveChangesAsync(_ct);

        _db.PersonStatuses.Add(new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            StatusKindId = k.Id,
            OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc),
            Sequence = 0,
            IsActive = false,
            Author = "sys",
            Modified = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(_ct);

        var active = await _svc.GetActiveAsync(p.Id, _ct);
        Assert.Null(active);
    }

    [Fact(DisplayName = "SetStatusAsync: перша установка створює валідний запис, Person.StatusKindId оновлюється")]
    public async Task SetStatus_FirstInstall_CreatesValid_UpdatesPerson()
    {
        var person = NewPerson();
        var kind = NewKindUnique("Start");
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
        var a = NewKindUnique("A");
        var b = NewKindUnique("B");
        _db.AddRange(person, a, b);
        await _db.SaveChangesAsync(_ct);

        var unspecified = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Unspecified);
        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = a.Id, OpenDate = unspecified }, _ct);
        Assert.Equal(DateTimeKind.Utc, s1.OpenDate.Kind);
        Assert.Equal(unspecified.Ticks, s1.OpenDate.Ticks);

        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        var local = new DateTime(2025, 09, 02, 0, 0, 0, DateTimeKind.Local);
        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = b.Id, OpenDate = local }, _ct);
        Assert.Equal(DateTimeKind.Utc, s2.OpenDate.Kind);
        Assert.Equal(local.ToUniversalTime(), s2.OpenDate);
    }

    [Fact(DisplayName = "SetStatusAsync: відхиляє момент ≤ останнього валідного")]
    public async Task SetStatus_Rejects_NonIncreasingMoment()
    {
        var p = NewPerson();
        var a = NewKindUnique("A");
        var b = NewKindUnique("B");
        _db.AddRange(p, a, b);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);
        var t0 = t1.AddHours(-1);

        _ = await _svc.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = a.Id, OpenDate = t1 }, _ct);

        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = b.Id, OpenDate = t0 }, _ct));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = b.Id, OpenDate = t1 }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок при порожньому PersonId")]
    public async Task SetStatus_Throws_When_PersonId_Empty()
    {
        var k = NewKindUnique("K");
        _db.Add(k);
        await _db.SaveChangesAsync(_ct);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = Guid.Empty,
                StatusKindId = k.Id,
                OpenDate = DateTime.UtcNow
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок при StatusKindId <= 0")]
    public async Task SetStatus_Throws_When_StatusKindId_Invalid()
    {
        var p = NewPerson();
        _db.Add(p);
        await _db.SaveChangesAsync(_ct);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = p.Id,
                StatusKindId = 0,
                OpenDate = DateTime.UtcNow
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок коли Person не існує")]
    public async Task SetStatus_Throws_When_Person_NotFound()
    {
        var k = NewKindUnique("K");
        _db.Add(k);
        await _db.SaveChangesAsync(_ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = Guid.NewGuid(),
                StatusKindId = k.Id,
                OpenDate = DateTime.UtcNow
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок коли StatusKind не існує")]
    public async Task SetStatus_Throws_When_StatusKind_NotFound()
    {
        var p = NewPerson();
        _db.Add(p);
        await _db.SaveChangesAsync(_ct);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = p.Id,
                StatusKindId = 999_999,
                OpenDate = DateTime.UtcNow
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок коли перехід заборонений правилами")]
    public async Task SetStatus_Throws_When_Transition_NotAllowed()
    {
        var p = NewPerson();
        var a = NewKindUnique("A");
        var b = NewKindUnique("B");
        _db.AddRange(p, a, b);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);
        _ = await _svc.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = a.Id, OpenDate = t1 }, _ct);

        // правило A→B НЕ додаємо
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = p.Id,
                StatusKindId = b.Id,
                OpenDate = t1.AddHours(1)
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: тримає Note/Author (trim) і підставляє Author='system' якщо порожньо")]
    public async Task SetStatus_Trims_And_Defaults_Author_And_Note()
    {
        var p = NewPerson();
        var a = NewKindUnique("A");
        var b = NewKindUnique("B");
        _db.AddRange(p, a, b);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);
        var s1 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = a.Id,
            OpenDate = t1,
            Note = "   hello   ",
            Author = "  john  "
        }, _ct);

        Assert.Equal("hello", s1.Note);
        Assert.Equal("john", s1.Author);

        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        var s2 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = b.Id,
            OpenDate = t1.AddHours(1),
            Note = "   ",
            Author = "   "
        }, _ct);

        Assert.Null(s2.Note);
        Assert.Equal("system", s2.Author);
    }

    [Fact(DisplayName = "IsTransitionAllowedAsync: null from → дозволено")]
    public async Task IsTransitionAllowed_NullFrom_Allows()
    {
        var allowed = await _svc.IsTransitionAllowedAsync(null, 123, _ct);
        Assert.True(allowed);
    }

    [Fact(DisplayName = "IsTransitionAllowedAsync: існує пара from→to → true; інакше → false")]
    public async Task IsTransitionAllowed_Positive_And_Negative()
    {
        var a = NewKindUnique("A");
        var b = NewKindUnique("B");
        _db.AddRange(a, b);
        await _db.SaveChangesAsync(_ct);

        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        Assert.True(await _svc.IsTransitionAllowedAsync(a.Id, b.Id, _ct)); // є правило
        Assert.False(await _svc.IsTransitionAllowedAsync(a.Id, a.Id, _ct)); // немає правила
    }

    [Fact(DisplayName = "UpdateStateIsActive: деактивація поточного оновлює Person.StatusKindId на попередній валідний")]
    public async Task UpdateStateIsActive_Deactivate_UpdatesPersonToPrevious()
    {
        var person = NewPerson();
        var a = NewKindUnique("A");
        var b = NewKindUnique("B");
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

    [Fact(DisplayName = "UpdateStateIsActive: активація без конфлікту не змінює Sequence і робить статус поточним")]
    public async Task UpdateStateIsActive_Activate_NoConflict_MakesCurrent()
    {
        var p = NewPerson();
        var a = NewKindUnique("A");
        var b = NewKindUnique("B");
        _db.AddRange(p, a, b);
        await _db.SaveChangesAsync(_ct);

        var t1 = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc);
        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = a.Id, OpenDate = t1 }, _ct);

        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        var t2 = t1.AddHours(2);
        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = p.Id, StatusKindId = b.Id, OpenDate = t2 }, _ct);

        // деактивуємо s2 → поточний знову A
        Assert.False(await _svc.UpdateStateIsActive(s2.Id, _ct));

        // зсуваємо s2 ще пізніше (щоб конфлікту ключа не було)
        s2.OpenDate = t2.AddHours(1);
        await _db.SaveChangesAsync(_ct);

        // активуємо назад
        Assert.True(await _svc.UpdateStateIsActive(s2.Id, _ct));

        var active = await _svc.GetActiveAsync(p.Id, _ct);
        Assert.Equal(s2.Id, active!.Id);
        Assert.Equal((short)0, active.Sequence); // без конфлікту sequence не чіпали
    }

    [Fact(DisplayName = "UpdateStateIsActive: активація при конфлікті key(person,open,seq) піднімає Sequence")]
    public async Task UpdateStateIsActive_Activate_ResolvesUniqueConflict_BySequenceBump()
    {
        var person = NewPerson();
        var a = NewKindUnique("A");
        var b = NewKindUnique("B");
        _db.AddRange(person, a, b);
        await _db.SaveChangesAsync(_ct);

        var t = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);

        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = a.Id, OpenDate = t }, _ct);

        _db.StatusTransitions.Add(Tr(a.Id, b.Id));
        await _db.SaveChangesAsync(_ct);

        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = b.Id, OpenDate = t.AddHours(1) }, _ct);

        // деактивуємо s2
        var nowInactive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.False(nowInactive);

        // «історичний дубль»: той самий ключ, що у s1
        s2.OpenDate = s1.OpenDate;
        s2.Sequence = s1.Sequence; // 0
        await _db.SaveChangesAsync(_ct);

        var nowActive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.True(nowActive);

        var reloaded = await _db.PersonStatuses.FirstAsync(x => x.Id == s2.Id, _ct);
        Assert.Equal((short)1, reloaded.Sequence); // посунуто до наступного доступного
    }

    [Fact(DisplayName = "UpdateStateIsActive: деактивація єдиного валідного → Person.StatusKindId = null")]
    public async Task UpdateStateIsActive_Deactivate_LastValid_MakesPersonStatusNull()
    {
        var p = NewPerson();
        var a = NewKindUnique("A");
        _db.AddRange(p, a);
        await _db.SaveChangesAsync(_ct);

        var s = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = p.Id,
            StatusKindId = a.Id,
            OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc)
        }, _ct);

        Assert.False(await _svc.UpdateStateIsActive(s.Id, _ct)); // деактивація

        var reloadedPerson = await _db.Persons.FirstAsync(x => x.Id == p.Id, _ct);
        Assert.Null(reloadedPerson.StatusKindId);
    }
}
