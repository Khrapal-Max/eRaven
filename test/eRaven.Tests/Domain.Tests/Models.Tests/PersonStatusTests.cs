//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonStatusTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Helpers;

namespace eRaven.Tests.Domain.Tests.Models.Tests;

public class PersonStatusTests
{
    [Fact]
    public void Create_WithNullToDate_IsActiveSemantics_AndValidationPasses()
    {
        // Arrange
        var s = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            FromDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ToDate = null,                 // активний інтервал (за семантикою)
            Note = "Будь-яка примітка",
            Author = "tester"
        };

        // Act
        var results = ValidationHelper.ValidateObject(s); // без DataAnnotations поверне порожньо

        // Assert
        Assert.Null(s.ToDate);
        Assert.Empty(results);
    }

    [Fact]
    public void Modified_Default_IsClrDefault_DateTime() // значення БД виставляє через defaultValueSql
    {
        // Arrange
        var s = new PersonStatus
        {
            PersonId = Guid.NewGuid(),
            StatusKindId = 1,
            FromDate = DateTime.UtcNow
            // Modified не задаємо — має бути default(DateTime) у CLR
        };

        // Act
        var modified = s.Modified;

        // Assert
        Assert.Equal(default, modified);
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
            FromDate = from,
            ToDate = to,
            Note = "ok",
            Author = "system",
            IsActive = true
        };

        // Act & Assert
        Assert.Equal(personId, s.PersonId);
        Assert.Equal(statusId, s.StatusKindId);
        Assert.Equal(from, s.FromDate);
        Assert.Equal(to, s.ToDate);
        Assert.Equal("ok", s.Note);
        Assert.Equal("system", s.Author);
        Assert.True(s.IsActive);
    }
}
