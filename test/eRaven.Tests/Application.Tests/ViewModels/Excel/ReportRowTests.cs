// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// ReportRowTests
// -----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace eRaven.Tests.Application.Tests.ViewModels.Excel;

public class ReportRowTests
{
    [Fact(DisplayName = "ReportRow: за замовчуванням всі властивості null")]
    public void Default_All_Properties_Are_Null()
    {
        var r = new ReportRow();

        Assert.Null(r.PositionCode);
        Assert.Null(r.PositionShort);
        Assert.Null(r.PositionFull);
        Assert.Null(r.SpecialNumber);

        Assert.Null(r.FullName);
        Assert.Null(r.Rank);
        Assert.Null(r.Rnokpp);
        Assert.Null(r.Callsign);
        Assert.Null(r.BZVP);
        Assert.Null(r.Weapon);

        Assert.Null(r.StatusCode);
        Assert.Null(r.StatusName);
        Assert.Null(r.StatusNote);
    }

    [Fact(DisplayName = "ReportRow: встановлення/зчитування властивостей працює")]
    public void Setters_Getters_Work()
    {
        var r = new ReportRow
        {
            PositionCode = "A-01",
            PositionShort = "Стрілець",
            PositionFull = "Стрілець Рота/Батальйон",
            SpecialNumber = "123",

            FullName = "Іваненко Іван Іванович",
            Rank = "сержант",
            Rnokpp = "1234567890",
            Callsign = "Шторм",
            BZVP = "БЗВП-42",
            Weapon = "АК-74",

            StatusCode = "30",
            StatusName = "В районі",
            StatusNote = "На місці"
        };

        Assert.Equal("A-01", r.PositionCode);
        Assert.Equal("Стрілець", r.PositionShort);
        Assert.Equal("Стрілець Рота/Батальйон", r.PositionFull);
        Assert.Equal("123", r.SpecialNumber);

        Assert.Equal("Іваненко Іван Іванович", r.FullName);
        Assert.Equal("сержант", r.Rank);
        Assert.Equal("1234567890", r.Rnokpp);
        Assert.Equal("Шторм", r.Callsign);
        Assert.Equal("БЗВП-42", r.BZVP);
        Assert.Equal("АК-74", r.Weapon);

        Assert.Equal("30", r.StatusCode);
        Assert.Equal("В районі", r.StatusName);
        Assert.Equal("На місці", r.StatusNote);
    }

    [Theory(DisplayName = "ReportRow: Display(Name) підписи коректні")]
    [InlineData(nameof(ReportRow.PositionCode), "Індекс")]
    [InlineData(nameof(ReportRow.PositionShort), "Посада (коротка)")]
    [InlineData(nameof(ReportRow.PositionFull), "Повна назва")]
    [InlineData(nameof(ReportRow.SpecialNumber), "ВОС")]
    [InlineData(nameof(ReportRow.FullName), "ПІБ")]
    [InlineData(nameof(ReportRow.Rank), "Звання")]
    [InlineData(nameof(ReportRow.Rnokpp), "РНОКПП")]
    [InlineData(nameof(ReportRow.Callsign), "Позивний")]
    [InlineData(nameof(ReportRow.BZVP), "БЗВП")]
    [InlineData(nameof(ReportRow.Weapon), "Зброя")]
    [InlineData(nameof(ReportRow.StatusCode), "Код статусу")]
    [InlineData(nameof(ReportRow.StatusName), "Статус")]
    [InlineData(nameof(ReportRow.StatusNote), "Нотатка")]
    public void Display_Attributes_Are_Correct(string propName, string expectedDisplayName)
    {
        var prop = typeof(ReportRow).GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);

        var display = prop!.GetCustomAttribute<DisplayAttribute>();
        Assert.NotNull(display);
        Assert.Equal(expectedDisplayName, display!.Name);
    }
}
