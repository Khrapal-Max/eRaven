//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PositionUnitTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Tests.Helpers;

namespace eRaven.Tests.Domain.Tests.Models.Tests;

public class PositionUnitTests
{
    [Fact]
    public void FullName_WithOrgPath_ComposesCorrectly()
    {
        var unit = new PositionUnit
        {
            ShortName = "механік",
            OrgPath = "цех А/відділ В"
        };

        var full = unit.FullName;

        Assert.Equal("механік цех А/відділ В", full);
    }

    [Fact]
    public void FullName_WithoutOrgPath_EqualsShortName()
    {
        var unit1 = new PositionUnit { ShortName = "механік", OrgPath = null };
        var unit2 = new PositionUnit { ShortName = "механік", OrgPath = "" };

        var f1 = unit1.FullName;
        var f2 = unit2.FullName;

        Assert.Equal("механік", f1);
        Assert.Equal("механік", f2);
    }

    [Fact]
    public void FullName_WhitespaceOrgPath_EqualsShortName()
    {
        var unit = new PositionUnit { ShortName = "механік", OrgPath = "   " };

        var full = unit.FullName;

        Assert.Equal("механік", full);
    }

    [Fact]
    public void People_Initialized_AsEmpty_AndNotSharedBetweenInstances()
    {
        var a = new PositionUnit { ShortName = "роль А" };
        var b = new PositionUnit { ShortName = "роль Б" };

        a.People.Add(new Person { Id = Guid.NewGuid(), LastName = "Іванов", FirstName = "Іван", Rnokpp = "1" });

        Assert.NotNull(a.People);
        Assert.Single(a.People);

        Assert.NotNull(b.People);
        Assert.Empty(b.People);

        Assert.NotSame(a.People, b.People);
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
    public void Defaults_ShortNameEmpty_CodeNull_OrgPathNull_AndPeopleNotNull()
    {
        var unit = new PositionUnit();

        Assert.Equal(string.Empty, unit.ShortName);
        Assert.Null(unit.Code);
        Assert.Null(unit.OrgPath);
        Assert.NotNull(unit.People);
        Assert.Empty(unit.People);
    }
}