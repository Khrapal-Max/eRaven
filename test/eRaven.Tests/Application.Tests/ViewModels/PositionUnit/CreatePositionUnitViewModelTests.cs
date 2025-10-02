//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePositionUnitViewModelTests
//-----------------------------------------------------------------------------
using System.ComponentModel.DataAnnotations;

namespace eRaven.Tests.Application.Tests.ViewModels.PositionUnit;

public class CreatePositionUnitViewModelTests
{
    // Допоміжний валідатор DataAnnotations
    private static List<ValidationResult> Validate(object instance)
    {
        var ctx = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Valid_Model_Passes_Validation()
    {
        var vm = new CreatePositionUnitViewModel
        {
            Code = "A1",
            ShortName = "Посада",
            SpecialNumber = "12-345",
            OrgPath = "Підрозділ / Відділ"
        };

        var results = Validate(vm);

        Assert.Empty(results);
    }

    [Fact]
    public void Missing_ShortName_Fails_Validation()
    {
        var vm = new CreatePositionUnitViewModel
        {
            Code = "A1",
            ShortName = "", // required
            SpecialNumber = "12-345",
            OrgPath = "Будь-який шлях"
        };

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames is not null &&
                                      r.MemberNames.Contains(nameof(CreatePositionUnitViewModel.ShortName)));
    }

    [Fact]
    public void MaxLength_Exceeded_Fails_For_ShortName_SpecialNumber_OrgPath()
    {
        var vm = new CreatePositionUnitViewModel
        {
            Code = "A1",
            ShortName = new string('X', 129),      // > 128
            SpecialNumber = new string('Y', 16),   // > 15
            OrgPath = new string('Z', 513)         // > 512
        };

        var results = Validate(vm);

        Assert.Equal(3, results.Count);
        Assert.Contains(results, r => r.MemberNames is not null &&
                                      r.MemberNames.Contains(nameof(CreatePositionUnitViewModel.ShortName)));
        Assert.Contains(results, r => r.MemberNames is not null &&
                                      r.MemberNames.Contains(nameof(CreatePositionUnitViewModel.SpecialNumber)));
        Assert.Contains(results, r => r.MemberNames is not null &&
                                      r.MemberNames.Contains(nameof(CreatePositionUnitViewModel.OrgPath)));
    }
}
