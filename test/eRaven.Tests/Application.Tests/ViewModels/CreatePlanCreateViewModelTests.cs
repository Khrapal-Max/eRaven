//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// CreatePlanCreateViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Tests.ViewModels;

public class CreatePlanCreateViewModelTests
{
    [Fact(DisplayName = "CreatePlanCreateViewModel: CLR-дефолти та порожній склад")]
    public void Defaults_Are_Clr_And_Empty_Participants()
    {
        var vm = new CreatePlanCreateViewModel();

        Assert.Null(vm.PlanNumber);
        Assert.Equal(default, vm.Type);          // 0
        Assert.Equal(default, vm.PlannedAt);     // 0001-01-01
        Assert.Equal(default, vm.TimeKind);      // 0

        Assert.Null(vm.Location);
        Assert.Null(vm.GroupName);
        Assert.Null(vm.ToolType);
        Assert.Null(vm.TaskDescription);
        Assert.Null(vm.Author);

        Assert.NotNull(vm.ParticipantIds);
        Assert.Empty(vm.ParticipantIds);
    }

    [Fact(DisplayName = "CreatePlanCreateViewModel: присвоєння властивостей (у т.ч. дата Local/Unspecified та дублікати учасників)")]
    public void Set_And_Read_Properties_With_DateKinds_And_Duplicates()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        // Unspecified час — сервіс потім нормалізує в UTC; VM просто зберігає значення
        var whenUnspecified = new DateTime(2025, 9, 15, 13, 30, 0, DateTimeKind.Unspecified);

        var vm = new CreatePlanCreateViewModel
        {
            PlanNumber = "PLAN-42",
            Type = PlanType.Dispatch,
            PlannedAt = whenUnspecified,
            TimeKind = PlanTimeKind.Start,
            Location = "Локація",
            GroupName = "Група-1",
            ToolType = "ТЗ",
            TaskDescription = "Опис робіт",
            Author = "tester",
            // навмисно з дублями — VM не дедуплікує, це робить сервіс
            ParticipantIds = [id1, id1, id2]
        };

        Assert.Equal("PLAN-42", vm.PlanNumber);
        Assert.Equal(PlanType.Dispatch, vm.Type);
        Assert.Equal(whenUnspecified, vm.PlannedAt);
        Assert.Equal(DateTimeKind.Unspecified, vm.PlannedAt.Kind);
        Assert.Equal(PlanTimeKind.Start, vm.TimeKind);
        Assert.Equal("Локація", vm.Location);
        Assert.Equal("Група-1", vm.GroupName);
        Assert.Equal("ТЗ", vm.ToolType);
        Assert.Equal("Опис робіт", vm.TaskDescription);
        Assert.Equal("tester", vm.Author);

        Assert.Equal(3, vm.ParticipantIds.Count);
        Assert.Collection(vm.ParticipantIds,
            g => Assert.Equal(id1, g),
            g => Assert.Equal(id1, g),
            g => Assert.Equal(id2, g));

        // додатково перевіримо, що VM приймає Local-час без змін
        var localTime = new DateTime(2025, 9, 16, 8, 0, 0, DateTimeKind.Local);
        vm.PlannedAt = localTime;
        Assert.Equal(DateTimeKind.Local, vm.PlannedAt.Kind);
        Assert.Equal(localTime, vm.PlannedAt);
    }
}
