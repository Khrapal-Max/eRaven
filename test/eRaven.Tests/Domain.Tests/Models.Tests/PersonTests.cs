//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonTests
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

        var fullName = person.FullName;

        Assert.Equal("Шевченко Тарас Григорович", fullName);
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

        var fullName = person.FullName;

        Assert.Equal("Шевченко Тарас", fullName);
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

        var fullName = person.FullName;

        Assert.Equal("Шевченко", fullName);
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

        var fullName = person.FullName;

        Assert.Equal("Шевченко", fullName);
    }

    // ---------- Колекції/ініціалізація ----------

    [Fact]
    public void StatusHistory_Initialized_AsEmptyCollection()
    {
        var person = new Person();

        Assert.NotNull(person.StatusHistory);
        Assert.Empty(person.StatusHistory);
    }
}
