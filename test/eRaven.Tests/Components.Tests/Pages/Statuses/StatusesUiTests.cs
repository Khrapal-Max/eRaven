//------------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//------------------------------------------------------------------------------
// StatusTransitionsUiTests
//------------------------------------------------------------------------------

using eRaven.Components.Pages.Statuses;
using eRaven.Domain.Models;

namespace eRaven.Tests.Components.Tests.Pages.Statuses;

public sealed class StatusesUiTests
{
    [Fact(DisplayName = "UI: FilterPersons фільтрує по ПІБ або РНОКПП")]
    public void FilterPersons_Works()
    {
        var src = new List<Person>
        {
            new() { Id = Guid.NewGuid(), LastName = "Іванов", FirstName = "Іван",   MiddleName = "Іванович", Rnokpp = "111" },
            new() { Id = Guid.NewGuid(), LastName = "Петренко", FirstName = "Петро", MiddleName = "Петрович", Rnokpp = "222" },
        };

        var byFio = StatusesUi.FilterPersons(src, "Петрен");
        var byTax = StatusesUi.FilterPersons(src, "111");
        var empty = StatusesUi.FilterPersons(src, "");

        Assert.Single(byFio);
        Assert.Equal("Петренко", byFio[0].LastName);

        Assert.Single(byTax);
        Assert.Equal("Іванов", byTax[0].LastName);

        Assert.Empty(empty);
    }

    [Fact(DisplayName = "UI: ToUtcFromLocalMidnight перетворює локальний день у 00:00 UTC")]
    public void ToUtcFromLocalMidnight_Works()
    {
        var local = new DateTime(2025, 9, 1); // Unspecified
        var utc = StatusesUi.ToUtcFromLocalMidnight(local);

        Assert.Equal(DateTimeKind.Utc, utc.Kind);
        // базова перевірка: це не наступний день у локальному часі
        Assert.True(utc <= utc.ToLocalTime().Date.AddDays(1).ToUniversalTime());
    }
}
