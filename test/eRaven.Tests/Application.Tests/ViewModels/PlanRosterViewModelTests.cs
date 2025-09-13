// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanRosterViewModelTests (smoke; перевірка дефолтів та set/get)
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;

namespace eRaven.Tests.Application.Tests.ViewModels;

public class PlanRosterViewModelTests
{
    [Fact(DisplayName = "Roster: дефолти — GUID пустий, рядки null, LastPlannedAction == null")]
    public void Defaults_AreCorrect()
    {
        var vm = new PlanRosterViewModel();

        Assert.Equal(Guid.Empty, vm.PersonId);

        // Повинні бути null за замовчуванням (init з default! тільки вимикає warning)
        Assert.Null(vm.FullName);
        Assert.Null(vm.Rnokpp);
        Assert.Null(vm.Rank);
        Assert.Null(vm.Position);

        Assert.Null(vm.StatusKindCode);
        Assert.Null(vm.StatusKindName);
    }

    [Fact(DisplayName = "Roster: можна задати та прочитати всі поля")]
    public void Can_Set_And_Read_All_Properties()
    {
        var personId = Guid.NewGuid();

        var vm = new PlanRosterViewModel
        {
            PersonId = personId,
            FullName = "Бут Сергій Олександрович",
            Rnokpp = "1234567890",
            Rank = "ст. солдат",
            Position = "Оператор БПЛА",

            StatusKindCode = "AREA",
            StatusKindName = "В районі"
        };

        Assert.Equal(personId, vm.PersonId);
        Assert.Equal("Бут Сергій Олександрович", vm.FullName);
        Assert.Equal("1234567890", vm.Rnokpp);
        Assert.Equal("ст. солдат", vm.Rank);
        Assert.Equal("Оператор БПЛА", vm.Position);

        Assert.Equal("AREA", vm.StatusKindCode);
        Assert.Equal("В районі", vm.StatusKindName);
    }
}
