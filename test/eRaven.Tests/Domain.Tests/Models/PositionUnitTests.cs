// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// -----------------------------------------------------------------------------
// PositionUnitTests
// -----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Helpers;

namespace eRaven.Tests.Domain.Tests.Models;

public class PositionUnitTests
{
    [Fact]
    public void DefaultValues_AreClrDefaults_And_FullName_EqualsShortName()
    {
        var unit = new PositionUnit();

        Assert.Equal(default, unit.Id);
        Assert.Null(unit.Code);
        Assert.Equal(string.Empty, unit.ShortName);
        Assert.Null(unit.OrgPath);
        Assert.Empty(unit.SpecialNumber);
        Assert.Null(unit.CurrentPerson);

        // Нове поле: за замовчуванням false
        Assert.False(unit.IsActived);

        // OrgPath is null => FullName == ShortName
        Assert.Equal(unit.ShortName, unit.FullName);
        Assert.Equal(string.Empty, unit.FullName);
    }

    [Fact]
    public void IsActived_DefaultFalse_CanBeToggled()
    {
        var unit = new PositionUnit { ShortName = "механік" };

        Assert.False(unit.IsActived);

        unit.IsActived = true;
        Assert.True(unit.IsActived);

        unit.IsActived = false;
        Assert.False(unit.IsActived);
    }

    [Fact]
    public void FullName_WithOrgPath_ComposesCorrectly()
    {
        var unit = new PositionUnit
        {
            ShortName = "механік",
            OrgPath = "цех А/відділ В"
        };

        Assert.Equal("механік цех А/відділ В", unit.FullName);
    }

    [Fact]
    public void FullName_WithoutOrgPath_EqualsShortName()
    {
        var unit1 = new PositionUnit { ShortName = "механік", OrgPath = null };
        var unit2 = new PositionUnit { ShortName = "механік", OrgPath = "" };

        Assert.Equal("механік", unit1.FullName);
        Assert.Equal("механік", unit2.FullName);
    }

    [Fact]
    public void FullName_WhitespaceOrgPath_EqualsShortName()
    {
        var unit = new PositionUnit { ShortName = "механік", OrgPath = "   " };
        Assert.Equal("механік", unit.FullName);
    }

    [Fact]
    public void Changing_ShortName_Updates_FullName_When_No_OrgPath()
    {
        var unit = new PositionUnit { ShortName = "старе", OrgPath = null };
        Assert.Equal("старе", unit.FullName);

        unit.ShortName = "нове";
        Assert.Equal("нове", unit.FullName);
    }

    [Fact]
    public void Code_SetGet_Works()
    {
        var unit = new PositionUnit
        {
            ShortName = "механік",
            Code = "MECH-01"
        };

        Assert.Equal("MECH-01", unit.Code);
    }

    [Fact]
    public void Code_MayBeNull_ValidationPasses()
    {
        var unit = new PositionUnit
        {
            ShortName = "механік",
            OrgPath = "цех А/відділ В",
            Code = null
        };

        var results = ValidationHelper.ValidateObject(unit);
        Assert.Empty(results);
    }

    [Fact]
    public void CurrentPerson_Default_IsNull()
    {
        var unit = new PositionUnit();
        Assert.Null(unit.CurrentPerson);
    }

    [Fact]
    public void CurrentPerson_Navigation_CanBeAssigned_AndLinkedWithPerson()
    {
        var unit = new PositionUnit
        {
            Id = Guid.NewGuid(),
            ShortName = "Сапер",
            OrgPath = "Рота 1 / Взвод 2"
        };

        var person = new Person
        {
            Id = Guid.NewGuid(),
            LastName = "Петренко",
            FirstName = "Петро",
            PositionUnitId = unit.Id,
            PositionUnit = unit
        };

        unit.CurrentPerson = person;

        Assert.Same(person, unit.CurrentPerson);
        Assert.Same(unit, person.PositionUnit);
        Assert.Equal(unit.Id, person.PositionUnitId);
        Assert.Equal("Сапер Рота 1 / Взвод 2", unit.FullName);
    }
}
