//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanniingOnDateRowViewModelTests
//-----------------------------------------------------------------------------

using System.Text.Json;

namespace eRaven.Tests.Application.Tests.ViewModels.Reports;

public class PlanniingOnDateRowViewModelTests
{
    [Fact(DisplayName = "PlanniingOnDateRowViewModel: значення за замовчуванням коректні")]
    public void Defaults_Are_Correct()
    {
        // Act
        var vm = new PlanniingOnDateRowViewModel();

        // Assert
        Assert.Null(vm.RankName);
        Assert.Null(vm.FullName);
        Assert.Null(vm.Callsign);
        Assert.Null(vm.PlanActionName);
        Assert.Null(vm.Order);
        Assert.Null(vm.Note);

        // DateTime не nullable → за замовчуванням default(DateTime)
        Assert.Equal(default, vm.EffectiveAtUtc);
    }

    [Fact(DisplayName = "PlanniingOnDateRowViewModel: можна встановити всі властивості")]
    public void Can_Set_All_Properties()
    {
        // Arrange
        var when = new DateTime(2025, 9, 24, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var vm = new PlanniingOnDateRowViewModel
        {
            RankName = "Сержант",
            FullName = "Іваненко Іван Іванович",
            Callsign = "Сокіл",
            PlanActionName = "Рапорт №15",
            Order = "Н-77/25",
            EffectiveAtUtc = when,
            Note = "коментар"
        };

        // Assert
        Assert.Equal("Сержант", vm.RankName);
        Assert.Equal("Іваненко Іван Іванович", vm.FullName);
        Assert.Equal("Сокіл", vm.Callsign);
        Assert.Equal("Рапорт №15", vm.PlanActionName);
        Assert.Equal("Н-77/25", vm.Order);
        Assert.Equal(when, vm.EffectiveAtUtc);
        Assert.Equal("коментар", vm.Note);
    }

    [Fact(DisplayName = "PlanniingOnDateRowViewModel: JSON round-trip зберігає дані")]
    public void Json_RoundTrip_Preserves_Data()
    {
        // Arrange
        var src = new PlanniingOnDateRowViewModel
        {
            RankName = "Сержант",
            FullName = "Петров Петро",
            Callsign = "Вітер",
            PlanActionName = "Рапорт-42",
            Order = "Наказ-7",
            EffectiveAtUtc = new DateTime(2025, 9, 10, 8, 15, 0, DateTimeKind.Utc),
            Note = "примітка"
        };

        // Act
        var json = JsonSerializer.Serialize(src);
        var back = JsonSerializer.Deserialize<PlanniingOnDateRowViewModel>(json)!;

        // Assert
        Assert.NotNull(back);
        Assert.Equal(src.RankName, back.RankName);
        Assert.Equal(src.FullName, back.FullName);
        Assert.Equal(src.Callsign, back.Callsign);
        Assert.Equal(src.PlanActionName, back.PlanActionName);
        Assert.Equal(src.Order, back.Order);
        Assert.Equal(src.EffectiveAtUtc, back.EffectiveAtUtc);
        Assert.Equal(src.Note, back.Note);
    }
}
