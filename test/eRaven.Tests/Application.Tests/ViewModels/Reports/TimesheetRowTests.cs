//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetRowTests
//-----------------------------------------------------------------------------


//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetRowTests
//-----------------------------------------------------------------------------

namespace eRaven.Tests.Application.Tests.ViewModels.Reports;

public sealed class TimesheetRowTests
{
    [Fact(DisplayName = "TimesheetRow: конструктор за замовчуванням — Days не null, порожній")]
    public void Ctor_Default_Sets_EmptyDays()
    {
        var row = new TimesheetRow();

        Assert.Equal(Guid.Empty, row.PersonId);
        Assert.Null(row.FullName);
        Assert.Null(row.Rank);
        Assert.Null(row.Rnokpp);

        Assert.NotNull(row.Days);
        Assert.Empty(row.Days);
    }

    [Fact(DisplayName = "TimesheetRow: встановлення властивостей і заповнення Days")]
    public void Properties_Can_Be_Set_And_Read()
    {
        var id = Guid.NewGuid();
        var row = new TimesheetRow
        {
            PersonId = id,
            FullName = "Іваненко Іван",
            Rank = "ст. солдат",
            Rnokpp = "1234567890",
            Days =
            [
                new DayCell { Code = "нб", Title = "Переміщення", Note = null },
                new DayCell { Code = "30", Title = "В районі", Note = "чергування" }
            ]
        };

        Assert.Equal(id, row.PersonId);
        Assert.Equal("Іваненко Іван", row.FullName);
        Assert.Equal("ст. солдат", row.Rank);
        Assert.Equal("1234567890", row.Rnokpp);

        Assert.NotNull(row.Days);
        Assert.Equal(2, row.Days.Length);

        Assert.Equal("нб", row.Days[0].Code);
        Assert.Equal("Переміщення", row.Days[0].Title);
        Assert.Null(row.Days[0].Note);

        Assert.Equal("30", row.Days[1].Code);
        Assert.Equal("В районі", row.Days[1].Title);
        Assert.Equal("чергування", row.Days[1].Note);
    }

    [Fact(DisplayName = "TimesheetRow: масив Days можна перевстановлювати будь-якої довжини")]
    public void Days_Array_Can_Be_Replaced_With_Any_Length()
    {
        var row = new TimesheetRow
        {
            // вересень (30 днів)
            Days = new DayCell[30]
        };
        row.Days[0] = new DayCell { Code = "нб" };
        row.Days[10] = new DayCell { Code = "30" };

        Assert.Equal(30, row.Days.Length);
        Assert.Equal("нб", row.Days[0].Code);
        Assert.Equal("30", row.Days[10].Code);

        // жовтень (31 день) — можна перевстановити
        row.Days = new DayCell[31];
        Assert.Equal(31, row.Days.Length);
        Assert.Null(row.Days[0]); // новий масив, попередні значення не зберігаються
    }
}
