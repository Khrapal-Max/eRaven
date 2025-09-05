//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonTests (повне покриття властивостей)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models.Tests;

public class PersonTests
{
    // ---------- FullName ----------

    [Fact]
    public void FullName_AllParts_ComposesCorrectly()
    {
        var person = new Person
        {
            LastName = "Шевченко",
            FirstName = "Тарас",
            MiddleName = "Григорович"
        };

        Assert.Equal("Шевченко Тарас Григорович", person.FullName);
    }

    [Fact]
    public void FullName_NoMiddleName_OmitsExtraSpace()
    {
        var person = new Person
        {
            LastName = "Шевченко",
            FirstName = "Тарас",
            MiddleName = null
        };

        Assert.Equal("Шевченко Тарас", person.FullName);
    }

    [Fact]
    public void FullName_OnlyLastName_ReturnsLastName()
    {
        var person = new Person
        {
            LastName = "Шевченко",
            FirstName = "",
            MiddleName = "   "
        };

        Assert.Equal("Шевченко", person.FullName);
    }

    [Fact]
    public void FullName_Ignores_Null_And_Whitespace_Parts()
    {
        var person = new Person
        {
            LastName = "Шевченко",
            FirstName = "   ",
            MiddleName = null
        };

        Assert.Equal("Шевченко", person.FullName);
    }

    [Fact]
    public void FullName_AllEmpty_ReturnsEmptyString()
    {
        var person = new Person
        {
            LastName = "",
            FirstName = "",
            MiddleName = null
        };

        Assert.Equal(string.Empty, person.FullName);
    }

    [Fact]
    public void FullName_Updates_When_Name_Parts_Change()
    {
        var p = new Person
        {
            LastName = "Іванов",
            FirstName = "Іван"
        };
        Assert.Equal("Іванов Іван", p.FullName);

        p.MiddleName = "Іванович";
        Assert.Equal("Іванов Іван Іванович", p.FullName);

        p.FirstName = "";
        Assert.Equal("Іванов Іванович", p.FullName);
    }

    // ---------- Defaults & Collections ----------

    [Fact]
    public void StatusHistory_Initialized_AsEmptyCollection()
    {
        var person = new Person();
        Assert.NotNull(person.StatusHistory);
        Assert.Empty(person.StatusHistory);
    }

    [Fact]
    public void PositionAssignments_Initialized_AsEmptyCollection()
    {
        var person = new Person();
        Assert.NotNull(person.PositionAssignments);
        Assert.Empty(person.PositionAssignments);
    }

    [Fact]
    public void Default_Scalars_Are_Assigned_As_Declared()
    {
        var p = new Person();
        Assert.Equal(string.Empty, p.Rnokpp);
        Assert.Equal(string.Empty, p.LastName);
        Assert.Equal(string.Empty, p.FirstName);

        Assert.Null(p.MiddleName);
        Assert.Null(p.BZVP);
        Assert.Null(p.Weapon);
        Assert.Null(p.Callsign);
        Assert.Null(p.PositionUnitId);
        Assert.Null(p.PositionUnit);

        Assert.Equal(0, p.StatusKindId); // int default
    }

    // ---------- Simple property set/get ----------

    [Fact]
    public void Can_Set_And_Get_All_Simple_Properties()
    {
        var pos = new PositionUnit
        {
            Id = Guid.NewGuid(),
            Code = "MECH-01",
            ShortName = "Механік",
            OrgPath = "Відділ В / Цех А"
        };
        var status = new StatusKind
        {
            Id = 3,
            Name = "Навчання",
            Code = "TRAIN",
            Order = 3,
            IsActive = true,
            Author = "sys",
            Modified = new DateTime(2025, 1, 5, 12, 0, 0, DateTimeKind.Utc)
        };

        var pid = pos.Id;

        var p = new Person
        {
            Id = Guid.NewGuid(),
            Rnokpp = "1234567890",
            Rank = "сержант",
            LastName = "Петренко",
            FirstName = "Петро",
            MiddleName = "Петрович",
            BZVP = "так",
            Weapon = "АК-74 №123",
            Callsign = "Сокіл",
            PositionUnitId = pid,
            PositionUnit = pos,
            StatusKindId = status.Id,
            StatusKind = status
        };

        Assert.Equal("1234567890", p.Rnokpp);
        Assert.Equal("сержант", p.Rank);
        Assert.Equal("Петренко", p.LastName);
        Assert.Equal("Петро", p.FirstName);
        Assert.Equal("Петрович", p.MiddleName);
        Assert.Equal("так", p.BZVP);
        Assert.Equal("АК-74 №123", p.Weapon);
        Assert.Equal("Сокіл", p.Callsign);

        Assert.Equal(pid, p.PositionUnitId);
        Assert.Same(pos, p.PositionUnit);

        Assert.Equal(status.Id, p.StatusKindId);
        Assert.Same(status, p.StatusKind);

        Assert.Equal("Петренко Петро Петрович", p.FullName);
    }

    // ---------- PositionUnit link semantics ----------

    [Fact]
    public void PositionUnit_Link_Is_Consistent_With_Id()
    {
        var unitId = Guid.NewGuid();
        var unit = new PositionUnit
        {
            Id = unitId,
            ShortName = "Сапер",
            OrgPath = "Рота 1 / Взвод 2"
        };

        var p = new Person
        {
            PositionUnitId = unitId,
            PositionUnit = unit
        };

        Assert.Equal(unitId, p.PositionUnitId);
        Assert.Same(unit, p.PositionUnit);
        Assert.Equal("Сапер Рота 1 / Взвод 2", unit.FullName);
    }

    // ---------- StatusHistory ops (basic) ----------

    [Fact]
    public void StatusHistory_Adds_Entries()
    {
        var p = new Person { Id = Guid.NewGuid() };

        var statusFirst = Guid.NewGuid();
        var statusLast = Guid.NewGuid();

        var s1 = new PersonStatus
        {
            Id = statusFirst,
            PersonId = p.Id,
            StatusKindId = 1,
            FromDate = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc)
        };
        var s2 = new PersonStatus
        {
            Id = statusLast,
            PersonId = p.Id,
            StatusKindId = 2,
            FromDate = new DateTime(2025, 1, 2, 9, 0, 0, DateTimeKind.Utc)
        };

        p.StatusHistory.Add(s1);
        p.StatusHistory.Add(s2);

        Assert.Equal(2, p.StatusHistory.Count);
        Assert.Contains(p.StatusHistory, x => x.Id == statusFirst && x.StatusKindId == 1);
        Assert.Contains(p.StatusHistory, x => x.Id == statusLast && x.StatusKindId == 2);
    }

    [Fact]
    public void StatusHistory_CanContain_Closed_And_Open_Statuses()
    {
        var p = new Person { Id = Guid.NewGuid() };
        var statusFirst = Guid.NewGuid();
        var statusLast = Guid.NewGuid();

        var open = new PersonStatus
        {
            Id = statusFirst,
            PersonId = p.Id,
            StatusKindId = 1,
            FromDate = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc),
            ToDate = null
        };
        var closed = new PersonStatus
        {
            Id = statusLast,
            PersonId = p.Id,
            StatusKindId = 2,
            FromDate = new DateTime(2025, 1, 5, 9, 0, 0, DateTimeKind.Utc),
            ToDate = new DateTime(2025, 1, 6, 18, 0, 0, DateTimeKind.Utc)
        };

        p.StatusHistory.Add(closed);
        p.StatusHistory.Add(open);

        Assert.Contains(p.StatusHistory, x => x.Id == statusFirst && x.ToDate == null);
        Assert.Contains(p.StatusHistory, x => x.Id == statusLast && x.ToDate != null);
    }

    // ---------- PositionAssignments (історія посад) ----------

    [Fact]
    public void PositionAssignments_Adds_Entries()
    {
        var p = new Person { Id = Guid.NewGuid() };
        var pos1 = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Оператор", OrgPath = "Рота А" };
        var pos2 = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Механік", OrgPath = "Рота Б" };

        var a1 = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            PositionUnitId = pos1.Id,
            PositionUnit = pos1,
            FromUtc = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2025, 1, 10, 18, 0, 0, DateTimeKind.Utc),
            Note = "попередня",
            Author = "tester",
            ModifiedUtc = new DateTime(2025, 1, 10, 19, 0, 0, DateTimeKind.Utc)
        };

        var a2 = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            PositionUnitId = pos2.Id,
            PositionUnit = pos2,
            FromUtc = new DateTime(2025, 1, 11, 8, 0, 0, DateTimeKind.Utc),
            ToUtc = null, // активна
            Note = "поточна",
            Author = "tester",
            ModifiedUtc = new DateTime(2025, 1, 11, 8, 5, 0, DateTimeKind.Utc)
        };

        p.PositionAssignments.Add(a1);
        p.PositionAssignments.Add(a2);

        Assert.Equal(2, p.PositionAssignments.Count);
        Assert.Contains(p.PositionAssignments, x => x.PositionUnitId == pos1.Id && x.ToUtc != null);
        Assert.Contains(p.PositionAssignments, x => x.PositionUnitId == pos2.Id && x.ToUtc == null);
        Assert.All(p.PositionAssignments, x => Assert.Equal(p.Id, x.PersonId));
    }

    [Fact]
    public void PositionAssignments_OpenAndClosed_Semantics()
    {
        var p = new Person { Id = Guid.NewGuid() };
        var pos = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Сапер", OrgPath = "Взвод 2" };

        var open = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            PositionUnitId = pos.Id,
            PositionUnit = pos,
            FromUtc = new DateTime(2025, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            ToUtc = null
        };
        var closed = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            PositionUnitId = pos.Id,
            PositionUnit = pos,
            FromUtc = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            ToUtc = new DateTime(2025, 1, 31, 18, 0, 0, DateTimeKind.Utc)
        };

        p.PositionAssignments.Add(closed);
        p.PositionAssignments.Add(open);

        Assert.Contains(p.PositionAssignments, x => x.ToUtc == null);
        Assert.Contains(p.PositionAssignments, x => x.ToUtc != null);
        Assert.True(closed.ToUtc > closed.FromUtc);
    }

    [Fact]
    public void PositionAssignments_Links_Are_Consistent()
    {
        var p = new Person { Id = Guid.NewGuid() };
        var pos = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Радист", OrgPath = "Рота 3" };

        var a = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            Person = p,
            PositionUnitId = pos.Id,
            PositionUnit = pos,
            FromUtc = new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc)
        };

        p.PositionAssignments.Add(a);

        var saved = p.PositionAssignments.Single();
        Assert.Same(p, saved.Person);
        Assert.Same(pos, saved.PositionUnit);
        Assert.Equal(p.Id, saved.PersonId);
        Assert.Equal(pos.Id, saved.PositionUnitId);
        Assert.Equal("Радист Рота 3", pos.FullName);
    }
}
