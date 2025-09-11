//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CreatePlanViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Application.Tests.ViewModels;

public sealed class CreatePlanViewModelTests
{
    [Fact(DisplayName = "CreatePlanViewModel: дефолти — PlanNumber=null, State=Open, PlanElements порожня колекція")]
    public void Defaults_AreCorrect()
    {
        // Act
        var vm = new CreatePlanViewModel();

        // Assert
        Assert.Null(vm.PlanNumber);                    // default! → у new() буде null
        Assert.Equal(PlanState.Open, vm.State);
        Assert.NotNull(vm.PlanElements);
        Assert.Empty(vm.PlanElements);
    }

    [Fact(DisplayName = "CreatePlanViewModel: можна призначити значення і прочитати їх")]
    public void Can_Assign_And_Read_Properties()
    {
        // Arrange
        var el1 = new PlanElement
        {
            Id = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            Type = PlanType.Dispatch,
            EventAtUtc = new DateTime(2025, 9, 10, 12, 0, 0, DateTimeKind.Utc),
            Location = "Локація А",
            GroupName = "Група А",
            ToolType = "Екіпаж А",
            Author = "tester"
            // Participants залишаємо за замовчуванням (порожньо) — для ViewModel це ок
        };

        var el2 = new PlanElement
        {
            Id = Guid.NewGuid(),
            PlanId = el1.PlanId,
            Type = PlanType.Return,
            EventAtUtc = new DateTime(2025, 9, 11, 8, 0, 0, DateTimeKind.Utc),
            Location = "Локація B",
            GroupName = "Група B",
            ToolType = "Екіпаж B",
            Author = "tester"
        };

        // Act
        var vm = new CreatePlanViewModel
        {
            PlanNumber = "R10/1CN",
            State = PlanState.Open,
            PlanElements = [el1, el2]
        };

        // Assert
        Assert.Equal("R10/1CN", vm.PlanNumber);
        Assert.Equal(PlanState.Open, vm.State);
        Assert.Equal(2, vm.PlanElements.Count);
        Assert.Contains(el1, vm.PlanElements);
        Assert.Contains(el2, vm.PlanElements);
    }

    [Fact(DisplayName = "CreatePlanViewModel: PlanElements підтримує різні колекції (масив/список)")]
    public void PlanElements_Allows_Different_Collections()
    {
        var e = new PlanElement
        {
            Id = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            Type = PlanType.Dispatch,
            EventAtUtc = new DateTime(2025, 9, 10, 0, 0, 0, DateTimeKind.Utc)
        };

        // Масив
        var vm1 = new CreatePlanViewModel
        {
            PlanNumber = "P-001",
            PlanElements = [e]
        };
        Assert.Single(vm1.PlanElements);

        // Список
        var vm2 = new CreatePlanViewModel
        {
            PlanNumber = "P-002",
            PlanElements = [e]
        };
        Assert.Single(vm2.PlanElements);
    }

    [Fact(DisplayName = "CreatePlanViewModel: допускає порожній номер (валідації тут немає)")]
    public void PlanNumber_Allows_Empty_AsRawInput()
    {
        var vm = new CreatePlanViewModel
        {
            PlanNumber = "",           // перевірка: ViewModel не застосовує трім/валідацію
            PlanElements = []
        };

        Assert.Equal(string.Empty, vm.PlanNumber);
        Assert.Empty(vm.PlanElements);
    }
}
