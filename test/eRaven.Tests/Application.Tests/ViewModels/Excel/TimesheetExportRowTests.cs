//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetExportRowTests
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetExportRowTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.TimesheetViewModels;

namespace eRaven.Tests.Application.Tests.ViewModels.Excel;

public class TimesheetExportRowTests
{
    [Fact(DisplayName = "TimesheetExportRow: конструктор за замовчуванням — поля null, Days порожній")]
    public void Ctor_Default_State()
    {
        var row = new TimesheetExportRow();

        Assert.Null(row.FullName);
        Assert.Null(row.Rank);
        Assert.Null(row.Rnokpp);
        Assert.NotNull(row.Days);
        Assert.Empty(row.Days);
    }

    [Fact(DisplayName = "TimesheetExportRow: властивості встановлюються та читаються")]
    public void Properties_Set_And_Get()
    {
        var row = new TimesheetExportRow
        {
            FullName = "Іваненко Іван",
            Rank = "сержант",
            Rnokpp = "1234567890",
            Days = ["30", "В", null!, "нб"]
        };

        Assert.Equal("Іваненко Іван", row.FullName);
        Assert.Equal("сержант", row.Rank);
        Assert.Equal("1234567890", row.Rnokpp);

        Assert.Equal(4, row.Days.Length);
        Assert.Equal("30", row.Days[0]);
        Assert.Equal("В", row.Days[1]);
        Assert.Null(row.Days[2]);
        Assert.Equal("нб", row.Days[3]);
    }

    [Fact(DisplayName = "TimesheetExportRow: допускає заміну масиву Days (інша довжина)")]
    public void Days_Array_Can_Be_Replaced_With_Different_Length()
    {
        var row = new TimesheetExportRow
        {
            Days = new string[31] // умовні 31 день
        };

        // перевстановлюємо на 30 днів (наприклад, вересень)
        var newDays = new string[30];
        newDays[10] = "30";
        row.Days = newDays;

        Assert.Same(newDays, row.Days);
        Assert.Equal(30, row.Days.Length);
        Assert.Equal("30", row.Days[10]);
    }

    [Fact(DisplayName = "TimesheetExportRow: елементи Days можуть бути пустими/пробілами — це ок")]
    public void Days_Items_Can_Be_Empty_Or_Whitespace()
    {
        var row = new TimesheetExportRow
        {
            Days = ["", " ", "\t", "30"]
        };

        Assert.Equal("", row.Days[0]);
        Assert.Equal(" ", row.Days[1]);
        Assert.Equal("\t", row.Days[2]);
        Assert.Equal("30", row.Days[3]);
    }
}
