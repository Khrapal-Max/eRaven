//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusKindTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class StatusKindTests
{
    [Fact]
    public void Defaults_AreExpected()
    {
        // Arrange
        var s = new StatusKind();

        // Act & Assert (перевіряємо CLR-дефолти, не БД)
        Assert.True(s.IsActive);          // у моделі = true
        Assert.Equal(0, s.Order);         // int default = 0 (БД теж має defaultValue(0))
        Assert.Null(s.Author);            // у моделі немає ініціалізатора; БД поставить "system" лише при insert
        Assert.Equal(default, s.Modified);// БД виставить значення через defaultValueSql під час insert
        Assert.Null(s.Name);              // default! лише прибирає warning, значення - null доки не встановимо
        Assert.Null(s.Code);              // те саме
        Assert.Equal(0, s.Id);            // int default
    }

    [Fact]
    public void CanSet_AndRead_AllProperties()
    {
        // Arrange
        var s = new StatusKind
        {
            Id = 7,
            Name = "Відрядження",
            Code = "ВДР",
            Order = 70,
            IsActive = true,
            Author = "tester",
            Modified = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc)
        };

        // Act & Assert
        Assert.Equal(7, s.Id);
        Assert.Equal("Відрядження", s.Name);
        Assert.Equal("ВДР", s.Code);
        Assert.Equal(70, s.Order);
        Assert.True(s.IsActive);
        Assert.Equal("tester", s.Author);
        Assert.Equal(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc), s.Modified);
    }

    [Fact]
    public void Toggle_IsActive_Works()
    {
        // Arrange
        var s = new StatusKind
        {
            // Act
            IsActive = false
        }; // IsActive == true за моделлю

        // Assert
        Assert.False(s.IsActive);
    }

    [Fact]
    public void Modified_Default_IsClrDefault_BeforeDbSetsValue()
    {
        // Arrange
        var s = new StatusKind();

        // Act
        var modified = s.Modified;

        // Assert
        // На рівні POCO очікуємо default(DateTime); реальне значення виставить БД під час вставки.
        Assert.Equal(default, modified);
    }
}