//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusTests (full coverage)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Helpers;

namespace eRaven.Tests.Domain.Tests.Models;

public class PersonStatusTests
{
    [Fact]
    public void DefaultValues_AreClrDefaults()
    {
        var s = new PersonStatus();

        Assert.Equal(default, s.Id);                 // Guid.Empty
        Assert.Equal(default, s.PersonId);           // Guid.Empty
        Assert.Equal(0, s.StatusKindId);
        Assert.Equal(default, s.OpenDate);           // 0001-01-01
        Assert.Null(s.CloseDate);
        Assert.Null(s.Note);
        Assert.False(s.IsActive);
        Assert.Null(s.Author);
        Assert.Equal(default, s.Modified);
        Assert.Null(s.Person);
        Assert.Null(s.StatusKind);
    }

    [Fact]
    public void Create_WithNullToDate_IsActiveSemantics_AndValidationPasses()
    {
        // Arrange
        var s = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            OpenDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            CloseDate = null, // відкритий інтервал
            Note = "Будь-яка примітка",
            Author = "tester"
        };

        // Act
        var results = ValidationHelper.ValidateObject(s); // без DataAnnotations — порожньо

        // Assert
        Assert.Null(s.CloseDate);
        Assert.Empty(results);
    }

    [Fact]
    public void Modified_Default_IsClrDefault_DateTime()
    {
        var s = new PersonStatus
        {
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            OpenDate = DateTime.UtcNow
        };

        Assert.Equal(default, s.Modified);
    }

    [Fact]
    public void CanSetAndReadBasicProperties()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var statusId = 7;
        var from = new DateTime(2025, 5, 10, 12, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        var s = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            StatusKindId = statusId,
            OpenDate = from,
            CloseDate = to,
            Note = "ok",
            Author = "system",
            IsActive = true
        };

        // Act & Assert
        Assert.Equal(personId, s.PersonId);
        Assert.Equal(statusId, s.StatusKindId);
        Assert.Equal(from, s.OpenDate);
        Assert.Equal(to, s.CloseDate);
        Assert.Equal("ok", s.Note);
        Assert.Equal("system", s.Author);
        Assert.True(s.IsActive);
    }

    [Fact]
    public void CanAssign_Navigation_References()
    {
        var p = new Person { Id = Guid.NewGuid(), LastName = "Петренко", FirstName = "Петро" };
        var k = new StatusKind { Id = 3, Name = "Навчання", Code = "TRAIN", Order = 3, IsActive = true };

        var s = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = p.Id,
            Person = p,
            StatusKindId = k.Id,
            StatusKind = k,
            OpenDate = new DateTime(2025, 1, 1, 8, 0, 0, DateTimeKind.Utc)
        };

        Assert.Equal(p.Id, s.PersonId);
        Assert.Same(p, s.Person);
        Assert.Equal(k.Id, s.StatusKindId);
        Assert.Same(k, s.StatusKind);
    }

    [Fact]
    public void Interval_Open_And_Closed_Semantics()
    {
        var open = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            OpenDate = new DateTime(2025, 1, 10, 10, 0, 0, DateTimeKind.Utc),
            CloseDate = null
        };
        var closed = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = open.PersonId,
            StatusKindId = 2,
            OpenDate = new DateTime(2025, 1, 5, 9, 0, 0, DateTimeKind.Utc),
            CloseDate = new DateTime(2025, 1, 6, 18, 0, 0, DateTimeKind.Utc)
        };

        Assert.Null(open.CloseDate);                 // відкритий інтервал
        Assert.NotNull(closed.CloseDate);            // закритий інтервал
        Assert.True(closed.CloseDate > closed.OpenDate);
    }

    [Fact]
    public void Validation_DoesNotEnforce_ToDate_After_FromDate_ByDefault()
    {
        // Наразі DataAnnotations відсутні — навіть якщо CloseDate < OpenDate, валідація порожня.
        var s = new PersonStatus
        {
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            OpenDate = new DateTime(2025, 1, 5, 10, 0, 0, DateTimeKind.Utc),
            CloseDate = new DateTime(2025, 1, 5, 9, 0, 0, DateTimeKind.Utc) // «некоректно», але без атрибутів не зламається
        };

        var results = ValidationHelper.ValidateObject(s);
        Assert.Empty(results);
    }

    [Fact]
    public void Toggle_IsActive_Works()
    {
        var s = new PersonStatus
        {
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            OpenDate = DateTime.UtcNow,
            IsActive = false
        };

        Assert.False(s.IsActive);
        s.IsActive = true;
        Assert.True(s.IsActive);
        s.IsActive = false;
        Assert.False(s.IsActive);
    }

    [Fact]
    public void Modified_CanBeAssigned()
    {
        var ts = new DateTime(2025, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        var s = new PersonStatus
        {
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            OpenDate = DateTime.UtcNow,
            Modified = ts
        };

        Assert.Equal(ts, s.Modified);
    }

    [Fact]
    public void Note_AllowsNullOrEmpty()
    {
        var s1 = new PersonStatus
        {
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            OpenDate = DateTime.UtcNow,
            Note = null
        };
        var s2 = new PersonStatus
        {
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            OpenDate = DateTime.UtcNow,
            Note = ""
        };

        Assert.Null(s1.Note);
        Assert.Equal(string.Empty, s2.Note);
    }
}
