// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanViewModelValidatorTests
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Components.Pages.Plans; // CreatePlanViewModelValidator
using FluentValidation.TestHelper;

namespace eRaven.Tests.Components.Tests.Pages.Plans;

public sealed class CreatePlanViewModelValidatorTests
{
    private readonly CreatePlanViewModelValidator _validator = new();

    [Fact(DisplayName = "PlanNumber: валідне значення — без помилок")]
    public void PlanNumber_Valid_Ok()
    {
        var vm = new CreatePlanViewModel { PlanNumber = "R10/1CN" };
        var result = _validator.TestValidate(vm);
        result.ShouldNotHaveValidationErrorFor(x => x.PlanNumber);
    }

    [Fact(DisplayName = "PlanNumber: null — помилка")]
    public void PlanNumber_Null_Fails()
    {
        var vm = new CreatePlanViewModel { PlanNumber = null! };
        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.PlanNumber);
    }

    [Fact(DisplayName = "PlanNumber: порожній рядок — помилка")]
    public void PlanNumber_Empty_Fails()
    {
        var vm = new CreatePlanViewModel { PlanNumber = string.Empty };
        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.PlanNumber);
    }

    [Fact(DisplayName = "PlanNumber: лише пробіли — помилка")]
    public void PlanNumber_WhitespaceOnly_Fails()
    {
        var vm = new CreatePlanViewModel { PlanNumber = "   " };
        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.PlanNumber);
    }

    [Fact(DisplayName = "PlanNumber: рівно 64 символи — ок")]
    public void PlanNumber_Exactly64_Ok()
    {
        var sixtyFour = new string('P', 64);
        var vm = new CreatePlanViewModel { PlanNumber = sixtyFour };
        var result = _validator.TestValidate(vm);
        result.ShouldNotHaveValidationErrorFor(x => x.PlanNumber);
    }

    [Fact(DisplayName = "PlanNumber: 65 символів — помилка довжини")]
    public void PlanNumber_TooLong_Fails()
    {
        var sixtyFive = new string('P', 65);
        var vm = new CreatePlanViewModel { PlanNumber = sixtyFive };
        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.PlanNumber);
    }
}
