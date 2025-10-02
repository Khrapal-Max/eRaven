//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// DayCellModelTests
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// DayCellModelTests
//-----------------------------------------------------------------------------

namespace eRaven.Tests.Application.Tests.ViewModels.Reports;

public class DayCellModelTests
{
    [Fact(DisplayName = "DayCell: конструктор за замовчуванням — усі поля null")]
    public void Ctor_Default_All_Null()
    {
        var cell = new DayCell();

        Assert.Null(cell.Code);
        Assert.Null(cell.Title);
        Assert.Null(cell.Note);
    }

    [Fact(DisplayName = "DayCell: властивості встановлюються та читаються")]
    public void Properties_Set_And_Get()
    {
        var cell = new DayCell
        {
            Code = "В",
            Title = "Відпустка",
            Note = "Наказ №123"
        };

        Assert.Equal("В", cell.Code);
        Assert.Equal("Відпустка", cell.Title);
        Assert.Equal("Наказ №123", cell.Note);
    }

    [Fact(DisplayName = "DayCell: підтримує перезапис значень (null/нові)")]
    public void Properties_Can_Be_Overwritten()
    {
        var cell = new DayCell
        {
            Code = "30",
            Title = "В районі",
            Note = "чергування"
        };

        // перезапис у null
        cell.Note = null;
        Assert.Null(cell.Note);

        // перезапис у інші значення
        cell.Code = "Л_Х";
        cell.Title = "Лікування по хворобі";

        Assert.Equal("Л_Х", cell.Code);
        Assert.Equal("Лікування по хворобі", cell.Title);
    }
}
