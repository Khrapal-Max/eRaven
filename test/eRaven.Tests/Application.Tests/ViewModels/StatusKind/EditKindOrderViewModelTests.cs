//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EditKindOrderViewModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Tests.Application.Tests.ViewModels.StatusKind;

public class EditKindOrderViewModelTests
{
    private static List<ValidationResult> Validate(EditKindOrderViewModel model)
    {
        var ctx = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, true);
        return results;
    }

    [Fact(DisplayName = "Valid model passes validation")]
    public void ValidModel_PassesValidation()
    {
        var model = new EditKindOrderViewModel
        {
            Id = 1,
            Name = "Статус X",
            CurrentOrder = 2,
            NewOrder = 3
        };

        var results = Validate(model);

        Assert.Empty(results);
    }

    [Fact(DisplayName = "Empty Name fails validation")]
    public void EmptyName_FailsValidation()
    {
        var model = new EditKindOrderViewModel
        {
            Id = 1,
            Name = "",
            CurrentOrder = 2,
            NewOrder = 3
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EditKindOrderViewModel.Name)));
    }

    [Fact(DisplayName = "Negative NewOrder fails validation")]
    public void NegativeNewOrder_FailsValidation()
    {
        var model = new EditKindOrderViewModel
        {
            Id = 1,
            Name = "Статус Y",
            CurrentOrder = 2,
            NewOrder = -5
        };

        var results = Validate(model);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(EditKindOrderViewModel.NewOrder)));
    }
}