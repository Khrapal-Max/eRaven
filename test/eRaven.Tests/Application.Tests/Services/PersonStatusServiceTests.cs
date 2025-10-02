//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PersonStatusServiceTests (Sequence + no CloseDate) — full coverage
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Application.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Application.Tests.Services;

public sealed class PersonStatusServiceTests : IDisposable
{
    private readonly SqliteDbHelper _helper = new();
    private readonly PersonStatusService _svc;
    private readonly CancellationToken _ct = default;

    public PersonStatusServiceTests()
    {
        // Фабрика контекстів всередині helper
        _svc = new PersonStatusService(_helper.Factory);
    }

    public void Dispose() => _helper.Dispose();

    // ---------- Helpers ----------

    private static long _rnokppSeed = 1000000000; // 10-значне число

    private static string NextRnokpp() =>
        Interlocked.Increment(ref _rnokppSeed).ToString();

    private static Person NewPerson() => new()
    {
        Id = Guid.NewGuid(),
        LastName = "Іванов",
        FirstName = "Іван",
        MiddleName = "Іванович",
        Rnokpp = NextRnokpp(),
        CreatedUtc = DateTime.UtcNow,
        ModifiedUtc = DateTime.UtcNow
    };

    private static StatusKind NewKindUnique(string? namePrefix = null) => new()
    {
        Id = 0,
        Code = $"K_{Guid.NewGuid():N}",
        Name = $"{namePrefix ?? "Kind"}_{Guid.NewGuid():N}",
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
        await using (var db = _helper.CreateContext())
        {
            var person = NewPerson();
            var kA = NewKindUnique("A");
            var kB = NewKindUnique("B");
            db.AddRange(person, kA, kB);
            await db.SaveChangesAsync(_ct);

            var t = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc);

            var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = person.Id, StatusKindId = kA.Id, OpenDate = t }, _ct);

            db.StatusTransitions.Add(Tr(kA.Id, kB.Id));
            await db.SaveChangesAsync(_ct);

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
            db.PersonStatuses.Add(s3);
            await db.SaveChangesAsync(_ct);
        }

        await using var verify = _helper.CreateContext();
        var all = (await _svc.GetAllAsync(_ct)).ToList();
        Assert.Equal(3, all.Count);

        // порядок: s2 (t+2h), s3 (t, seq=5), s1 (t, seq=0)
        var byOrder = all.Select(x => x).ToList();
        Assert.True(byOrder[0].OpenDate > byOrder[1].OpenDate
                    || (byOrder[0].OpenDate == byOrder[1].OpenDate && byOrder[0].Sequence > byOrder[1].Sequence));
        Assert.True(byOrder[1].OpenDate >= byOrder[2].OpenDate);

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
        Guid p1Id, p2Id; int kId;

        await using (var db = _helper.CreateContext())
        {
            var p1 = NewPerson();
            var p2 = NewPerson();
            var k = NewKindUnique("K");
            db.AddRange(p1, p2, k);
            await db.SaveChangesAsync(_ct);

            p1Id = p1.Id; p2Id = p2.Id; kId = k.Id;

            var t = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc);
            _ = await _svc.SetStatusAsync(new PersonStatus { PersonId = p1Id, StatusKindId = kId, OpenDate = t }, _ct);
            _ = await _svc.SetStatusAsync(new PersonStatus { PersonId = p2Id, StatusKindId = kId, OpenDate = t.AddHours(1) }, _ct);
        }

        var hist1 = await _svc.GetHistoryAsync(p1Id, _ct);
        Assert.Single(hist1);
        Assert.All(hist1, h => Assert.Equal(p1Id, h.PersonId));
    }

    [Fact(DisplayName = "GetActiveAsync: повертає null, якщо активних немає")]
    public async Task GetActiveAsync_ReturnsNull_WhenNoActive()
    {
        Guid pId; int kId;
        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            var k = NewKindUnique("K");
            db.AddRange(p, k);
            await db.SaveChangesAsync(_ct);

            pId = p.Id; kId = k.Id;

            db.PersonStatuses.Add(new PersonStatus
            {
                Id = Guid.NewGuid(),
                PersonId = pId,
                StatusKindId = kId,
                OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc),
                Sequence = 0,
                IsActive = false,
                Author = "sys",
                Modified = DateTime.UtcNow
            });
            await db.SaveChangesAsync(_ct);
        }

        var active = await _svc.GetActiveAsync(pId, _ct);
        Assert.Null(active);
    }

    [Fact(DisplayName = "SetStatusAsync: перша установка створює валідний запис, Person.StatusKindId оновлюється")]
    public async Task SetStatus_FirstInstall_CreatesValid_UpdatesPerson()
    {
        Guid personId; int kindId;

        await using (var db = _helper.CreateContext())
        {
            var person = NewPerson();
            var kind = NewKindUnique("Start");
            db.AddRange(person, kind);
            await db.SaveChangesAsync(_ct);
            personId = person.Id; kindId = kind.Id;
        }

        var saved = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = personId,
            StatusKindId = kindId,
            OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc)
        }, _ct);

        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.True(saved.IsActive);
        Assert.Equal((short)0, saved.Sequence);
        Assert.Equal(kindId, saved.StatusKindId);

        await using var verify = _helper.CreateContext();
        var reloadedPerson = await verify.Persons.FindAsync([personId], _ct);
        Assert.Equal(kindId, reloadedPerson!.StatusKindId);

        var active = await _svc.GetActiveAsync(personId, _ct);
        Assert.NotNull(active);
        Assert.Equal(saved.Id, active!.Id);
    }

    [Fact(DisplayName = "SetStatusAsync: нормалізує OpenDate до UTC (Unspecified/Local) і проходить A→B")]
    public async Task SetStatus_NormalizesOpenDateToUtc()
    {
        Guid personId; int aId, bId;

        await using (var db = _helper.CreateContext())
        {
            var person = NewPerson();
            var a = NewKindUnique("A");
            var b = NewKindUnique("B");
            db.AddRange(person, a, b);
            await db.SaveChangesAsync(_ct);
            personId = person.Id; aId = a.Id; bId = b.Id;
        }

        var unspecified = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Unspecified);
        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = personId, StatusKindId = aId, OpenDate = unspecified }, _ct);
        Assert.Equal(DateTimeKind.Utc, s1.OpenDate.Kind);
        Assert.Equal(unspecified.Ticks, s1.OpenDate.Ticks);

        await using (var db = _helper.CreateContext())
        {
            db.StatusTransitions.Add(Tr(aId, bId));
            await db.SaveChangesAsync(_ct);
        }

        var local = new DateTime(2025, 09, 02, 0, 0, 0, DateTimeKind.Local);
        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = personId, StatusKindId = bId, OpenDate = local }, _ct);
        Assert.Equal(DateTimeKind.Utc, s2.OpenDate.Kind);
        Assert.Equal(local.ToUniversalTime(), s2.OpenDate);
    }

    [Fact(DisplayName = "SetStatusAsync: дозволяє момент == останньому (підвищує Sequence) і відхиляє < останнього")]
    public async Task SetStatus_Allows_EqualMoment_IncrementsSequence_Blocks_PastMoment()
    {
        // Arrange
        Guid pId; int aId, bId;
        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);
        var t0 = t1.AddHours(-1);

        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            var a = NewKindUnique("A"); // from
            var b = NewKindUnique("B"); // to
            db.AddRange(p, a, b);
            await db.SaveChangesAsync(_ct);
            pId = p.Id; aId = a.Id; bId = b.Id;
        }

        // Початковий статус A @ t1 (sequence очікуємо 0)
        var first = await _svc.SetStatusAsync(
            new PersonStatus { PersonId = pId, StatusKindId = aId, OpenDate = t1 }, _ct);
        Assert.Equal((short)0, first.Sequence);

        // Дозволений перехід A -> B
        await using (var db = _helper.CreateContext())
        {
            db.StatusTransitions.Add(Tr(aId, bId));
            await db.SaveChangesAsync(_ct);
        }

        // 1) < останнього — має впасти
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus { PersonId = pId, StatusKindId = bId, OpenDate = t0 }, _ct));

        // 2) == останньому — дозволено, має підняти Sequence
        var second = await _svc.SetStatusAsync(
            new PersonStatus { PersonId = pId, StatusKindId = bId, OpenDate = t1 }, _ct);

        Assert.Equal(t1, second.OpenDate);
        Assert.Equal((short)1, second.Sequence); // після першого запису на той самий момент

        // Перевіримо, що збережено обидва в очікуваному порядку
        await using (var db = _helper.CreateContext())
        {
            var list = await db.PersonStatuses
                .Where(s => s.PersonId == pId)
                .OrderBy(s => s.OpenDate).ThenBy(s => s.Sequence)
                .ToListAsync(_ct);

            Assert.Equal(2, list.Count);
            Assert.Equal(aId, list[0].StatusKindId); // A @ t1 seq 0
            Assert.Equal((short)0, list[0].Sequence);

            Assert.Equal(bId, list[1].StatusKindId); // B @ t1 seq 1
            Assert.Equal((short)1, list[1].Sequence);
        }
    }


    [Fact(DisplayName = "SetStatusAsync: кидок при порожньому PersonId")]
    public async Task SetStatus_Throws_When_PersonId_Empty()
    {
        int kId;
        await using (var db = _helper.CreateContext())
        {
            var k = NewKindUnique("K");
            db.Add(k);
            await db.SaveChangesAsync(_ct);
            kId = k.Id;
        }

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = Guid.Empty,
                StatusKindId = kId,
                OpenDate = DateTime.UtcNow
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок при StatusKindId <= 0")]
    public async Task SetStatus_Throws_When_StatusKindId_Invalid()
    {
        Guid pId;
        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            db.Add(p);
            await db.SaveChangesAsync(_ct);
            pId = p.Id;
        }

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = pId,
                StatusKindId = 0,
                OpenDate = DateTime.UtcNow
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок коли Person не існує")]
    public async Task SetStatus_Throws_When_Person_NotFound()
    {
        int kId;
        await using (var db = _helper.CreateContext())
        {
            var k = NewKindUnique("K");
            db.Add(k);
            await db.SaveChangesAsync(_ct);
            kId = k.Id;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = Guid.NewGuid(),
                StatusKindId = kId,
                OpenDate = DateTime.UtcNow
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок коли StatusKind не існує")]
    public async Task SetStatus_Throws_When_StatusKind_NotFound()
    {
        Guid pId;
        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            db.Add(p);
            await db.SaveChangesAsync(_ct);
            pId = p.Id;
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = pId,
                StatusKindId = 999_999,
                OpenDate = DateTime.UtcNow
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: кидок коли перехід заборонений правилами")]
    public async Task SetStatus_Throws_When_Transition_NotAllowed()
    {
        Guid pId; int aId, bId;

        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            var a = NewKindUnique("A");
            var b = NewKindUnique("B");
            db.AddRange(p, a, b);
            await db.SaveChangesAsync(_ct);
            pId = p.Id; aId = a.Id; bId = b.Id;
        }

        var t1 = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);
        _ = await _svc.SetStatusAsync(new PersonStatus { PersonId = pId, StatusKindId = aId, OpenDate = t1 }, _ct);

        // правило A→B НЕ додаємо
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.SetStatusAsync(new PersonStatus
            {
                PersonId = pId,
                StatusKindId = bId,
                OpenDate = t1.AddHours(1)
            }, _ct));
    }

    [Fact(DisplayName = "SetStatusAsync: тримає Note/Author (trim) і підставляє Author='system' якщо порожньо")]
    public async Task SetStatus_Trims_And_Defaults_Author_And_Note()
    {
        Guid pId; int aId, bId;

        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            var a = NewKindUnique("A");
            var b = NewKindUnique("B");
            db.AddRange(p, a, b);
            await db.SaveChangesAsync(_ct);
            pId = p.Id; aId = a.Id; bId = b.Id;
        }

        var t1 = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc);
        var s1 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = pId,
            StatusKindId = aId,
            OpenDate = t1,
            Note = "   hello   ",
            Author = "  john  "
        }, _ct);

        Assert.Equal("hello", s1.Note);
        Assert.Equal("john", s1.Author);

        await using (var db = _helper.CreateContext())
        {
            db.StatusTransitions.Add(Tr(aId, bId));
            await db.SaveChangesAsync(_ct);
        }

        var s2 = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = pId,
            StatusKindId = bId,
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
        int aId, bId;
        await using (var db = _helper.CreateContext())
        {
            var a = NewKindUnique("A");
            var b = NewKindUnique("B");
            db.AddRange(a, b);
            await db.SaveChangesAsync(_ct);
            aId = a.Id; bId = b.Id;

            db.StatusTransitions.Add(Tr(aId, bId));
            await db.SaveChangesAsync(_ct);
        }

        Assert.True(await _svc.IsTransitionAllowedAsync(aId, bId, _ct)); // є правило
        Assert.False(await _svc.IsTransitionAllowedAsync(aId, aId, _ct)); // немає правила
    }

    [Fact(DisplayName = "UpdateStateIsActive: деактивація поточного оновлює Person.StatusKindId на попередній валідний")]
    public async Task UpdateStateIsActive_Deactivate_UpdatesPersonToPrevious()
    {
        Guid personId; int aId, bId;
        var t1 = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);

        await using (var db = _helper.CreateContext())
        {
            var person = NewPerson();
            var a = NewKindUnique("A");
            var b = NewKindUnique("B");
            db.AddRange(person, a, b);
            await db.SaveChangesAsync(_ct);
            personId = person.Id; aId = a.Id; bId = b.Id;
        }

        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = personId, StatusKindId = aId, OpenDate = t1 }, _ct);

        await using (var db = _helper.CreateContext())
        {
            db.StatusTransitions.Add(Tr(aId, bId));
            await db.SaveChangesAsync(_ct);
        }

        var t2 = t1.AddHours(1);
        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = personId, StatusKindId = bId, OpenDate = t2 }, _ct);

        var nowInactive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.False(nowInactive);

        await using var verify = _helper.CreateContext();
        var reloadedPerson = await verify.Persons.FirstAsync(x => x.Id == personId, _ct);
        Assert.Equal(aId, reloadedPerson!.StatusKindId);
    }

    [Fact(DisplayName = "UpdateStateIsActive: активація без конфлікту не змінює Sequence і робить статус поточним")]
    public async Task UpdateStateIsActive_Activate_NoConflict_MakesCurrent()
    {
        Guid pId; int aId, bId;

        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            var a = NewKindUnique("A");
            var b = NewKindUnique("B");
            db.AddRange(p, a, b);
            await db.SaveChangesAsync(_ct);
            pId = p.Id; aId = a.Id; bId = b.Id;
        }

        var t1 = new DateTime(2025, 09, 10, 0, 0, 0, DateTimeKind.Utc);
        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = pId, StatusKindId = aId, OpenDate = t1 }, _ct);

        await using (var db = _helper.CreateContext())
        {
            db.StatusTransitions.Add(Tr(aId, bId));
            await db.SaveChangesAsync(_ct);
        }

        var t2 = t1.AddHours(2);
        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = pId, StatusKindId = bId, OpenDate = t2 }, _ct);

        // деактивуємо s2 → поточний знову A
        Assert.False(await _svc.UpdateStateIsActive(s2.Id, _ct));

        // посунемо s2 ще пізніше, аби не було конфлікту
        await using (var db = _helper.CreateContext())
        {
            var s2Tracked = await db.PersonStatuses.FirstAsync(x => x.Id == s2.Id, _ct);
            s2Tracked.OpenDate = t2.AddHours(1);
            await db.SaveChangesAsync(_ct);
        }

        // активуємо назад
        Assert.True(await _svc.UpdateStateIsActive(s2.Id, _ct));

        var active = await _svc.GetActiveAsync(pId, _ct);
        Assert.Equal(s2.Id, active!.Id);
        Assert.Equal((short)0, active.Sequence);
    }

    [Fact(DisplayName = "UpdateStateIsActive: активація при конфлікті key(person,open,seq) піднімає Sequence")]
    public async Task UpdateStateIsActive_Activate_ResolvesUniqueConflict_BySequenceBump()
    {
        Guid pId; int aId, bId;
        var t = new DateTime(2025, 09, 10, 00, 00, 00, DateTimeKind.Utc);

        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            var a = NewKindUnique("A");
            var b = NewKindUnique("B");
            db.AddRange(p, a, b);
            await db.SaveChangesAsync(_ct);
            pId = p.Id; aId = a.Id; bId = b.Id;
        }

        var s1 = await _svc.SetStatusAsync(new PersonStatus { PersonId = pId, StatusKindId = aId, OpenDate = t }, _ct);

        await using (var db = _helper.CreateContext())
        {
            db.StatusTransitions.Add(Tr(aId, bId));
            await db.SaveChangesAsync(_ct);
        }

        var s2 = await _svc.SetStatusAsync(new PersonStatus { PersonId = pId, StatusKindId = bId, OpenDate = t.AddHours(1) }, _ct);

        // деактивуємо s2
        var nowInactive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.False(nowInactive);

        // «історичний дубль»: той самий ключ, що у s1
        await using (var db = _helper.CreateContext())
        {
            var s2Tracked = await db.PersonStatuses.FirstAsync(x => x.Id == s2.Id, _ct);
            var s1Tracked = await db.PersonStatuses.FirstAsync(x => x.Id == s1.Id, _ct);
            s2Tracked.OpenDate = s1Tracked.OpenDate;
            s2Tracked.Sequence = s1Tracked.Sequence; // 0
            await db.SaveChangesAsync(_ct);
        }

        var nowActive = await _svc.UpdateStateIsActive(s2.Id, _ct);
        Assert.True(nowActive);

        await using var verify = _helper.CreateContext();
        var reloaded = await verify.PersonStatuses.FirstAsync(x => x.Id == s2.Id, _ct);
        Assert.Equal((short)1, reloaded.Sequence);
    }

    [Fact(DisplayName = "UpdateStateIsActive: деактивація єдиного валідного → Person.StatusKindId = null")]
    public async Task UpdateStateIsActive_Deactivate_LastValid_MakesPersonStatusNull()
    {
        Guid pId; int aId;

        await using (var db = _helper.CreateContext())
        {
            var p = NewPerson();
            var a = NewKindUnique("A");
            db.AddRange(p, a);
            await db.SaveChangesAsync(_ct);
            pId = p.Id; aId = a.Id;
        }

        var s = await _svc.SetStatusAsync(new PersonStatus
        {
            PersonId = pId,
            StatusKindId = aId,
            OpenDate = new DateTime(2025, 09, 01, 0, 0, 0, DateTimeKind.Utc)
        }, _ct);

        Assert.False(await _svc.UpdateStateIsActive(s.Id, _ct)); // деактивація

        await using var verify = _helper.CreateContext();
        var reloadedPerson = await verify.Persons.FirstAsync(x => x.Id == pId, _ct);
        Assert.Null(reloadedPerson.StatusKindId);
    }
}
