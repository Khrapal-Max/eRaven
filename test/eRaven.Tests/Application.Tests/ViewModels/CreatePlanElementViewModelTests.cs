// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanElementViewModelTests — прості smoke-тести (домен рівень)
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Tests.ViewModels;

public sealed class CreatePlanElementViewModelTests
{
    [Fact(DisplayName = "VM: можна створити і прочитати всі властивості (Dispatch з контекстом)")]
    public void Can_Construct_And_Read_All_Properties_For_Dispatch()
    {
        // Arrange
        var when = new DateTime(2025, 9, 12, 12, 30, 0, DateTimeKind.Utc);
        var personId = Guid.NewGuid();

        // Act
        var vm = new CreatePlanElementViewModel
        {
            Type = PlanType.Dispatch,
            EventAtUtc = when,
            Location = "СТЕПОВЕ",
            GroupName = "МАЛІБУ",
            ToolType = "ФПВ",
            Note = "тест",
            PersonId = personId
        };

        // Assert
        Assert.Equal(PlanType.Dispatch, vm.Type);
        Assert.Equal(when, vm.EventAtUtc);
        Assert.Equal(DateTimeKind.Utc, vm.EventAtUtc.Kind);

        Assert.Equal("СТЕПОВЕ", vm.Location);
        Assert.Equal("МАЛІБУ", vm.GroupName);
        Assert.Equal("ФПВ", vm.ToolType);
        Assert.Equal("тест", vm.Note);
        Assert.Equal(personId, vm.PersonId);
    }

    [Fact(DisplayName = "VM: Return допускає null-контекст (Location/Group/Tool), сервіс підставить сам")]
    public void Return_Allows_Null_Context()
    {
        // Arrange
        var personId = Guid.NewGuid();
        var when = new DateTime(2025, 9, 13, 15, 30, 0, DateTimeKind.Utc);

        // Act
        var vm = new CreatePlanElementViewModel
        {
            Type = PlanType.Return,
            EventAtUtc = when,
            // свідомо не задаємо Location/GroupName/ToolType
            PersonId = personId
        };

        // Assert
        Assert.Equal(PlanType.Return, vm.Type);
        Assert.Equal(when, vm.EventAtUtc);
        Assert.Equal(DateTimeKind.Utc, vm.EventAtUtc.Kind);

        Assert.Null(vm.Location);
        Assert.Null(vm.GroupName);
        Assert.Null(vm.ToolType);
        Assert.Equal(personId, vm.PersonId);
    }
}
