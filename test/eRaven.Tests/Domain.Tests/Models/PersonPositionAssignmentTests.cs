//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonPositionAssignmentTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Helpers;

namespace eRaven.Tests.Domain.Tests.Models;

public class PersonPositionAssignmentTests
{
    [Fact]
    public void DefaultValues_AreClrDefaults()
    {
        var a = new PersonPositionAssignment();

        Assert.Equal(default, a.Id);
        Assert.Equal(default, a.PersonId);
        Assert.Null(a.Person);
        Assert.Equal(default, a.PositionUnitId);
        Assert.Null(a.PositionUnit);
        Assert.Equal(default, a.OpenUtc);
        Assert.Null(a.CloseUtc);
        Assert.Null(a.Note);
        Assert.Null(a.Author);
        Assert.Equal(default, a.ModifiedUtc);
    }

    [Fact]
    public void CanSetAndGetBasicProperties()
    {
        var personId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var from = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 1, 10, 18, 0, 0, DateTimeKind.Utc);
        var ts = new DateTime(2025, 1, 10, 19, 0, 0, DateTimeKind.Utc);

        var a = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            PositionUnitId = positionId,
            OpenUtc = from,
            CloseUtc = to,
            Note = "коментар",
            Author = "tester",
            ModifiedUtc = ts
        };

        Assert.Equal(personId, a.PersonId);
        Assert.Equal(positionId, a.PositionUnitId);
        Assert.Equal(from, a.OpenUtc);
        Assert.Equal(to, a.CloseUtc);
        Assert.Equal("коментар", a.Note);
        Assert.Equal("tester", a.Author);
        Assert.Equal(ts, a.ModifiedUtc);
    }

    [Fact]
    public void OpenAndClosed_Semantics()
    {
        var open = new PersonPositionAssignment
        {
            PersonId = Guid.NewGuid(),
            PositionUnitId = Guid.NewGuid(),
            OpenUtc = new DateTime(2025, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            CloseUtc = null
        };
        var closed = new PersonPositionAssignment
        {
            PersonId = open.PersonId,
            PositionUnitId = open.PositionUnitId,
            OpenUtc = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc),
            CloseUtc = new DateTime(2025, 1, 31, 18, 0, 0, DateTimeKind.Utc)
        };

        Assert.Null(open.CloseUtc);                     // активне закріплення
        Assert.NotNull(closed.CloseUtc);                // завершене
        Assert.True(closed.CloseUtc > closed.OpenUtc);  // хронологія
    }

    [Fact]
    public void Navigation_Links_Consistent()
    {
        var p = new Person { Id = Guid.NewGuid(), LastName = "Козак", FirstName = "Семен" };
        var u = new PositionUnit { Id = Guid.NewGuid(), ShortName = "Оператор", OrgPath = "Рота А" };

        var a = new PersonPositionAssignment
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            Person = p,
            PositionUnitId = u.Id,
            PositionUnit = u,
            OpenUtc = new DateTime(2025, 3, 1, 8, 0, 0, DateTimeKind.Utc)
        };

        Assert.Same(p, a.Person);
        Assert.Same(u, a.PositionUnit);
        Assert.Equal(p.Id, a.PersonId);
        Assert.Equal(u.Id, a.PositionUnitId);
        Assert.Equal("Оператор Рота А", u.FullName);
    }

    [Fact]
    public void Validation_NoDataAnnotations_ReturnsEmpty()
    {
        var a = new PersonPositionAssignment
        {
            PersonId = Guid.NewGuid(),
            PositionUnitId = Guid.NewGuid(),
            OpenUtc = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc),
            CloseUtc = new DateTime(2025, 1, 4, 10, 0, 0, DateTimeKind.Utc) // навіть так — без атрибутів помилки немає
        };

        var results = ValidationHelper.ValidateObject(a);
        Assert.Empty(results);
    }

    [Fact]
    public void ModifiedUtc_CanBeAssigned()
    {
        var stamp = new DateTime(2025, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        var a = new PersonPositionAssignment
        {
            PersonId = Guid.NewGuid(),
            PositionUnitId = Guid.NewGuid(),
            OpenUtc = DateTime.UtcNow,
            ModifiedUtc = stamp
        };

        Assert.Equal(stamp, a.ModifiedUtc);
    }
}
