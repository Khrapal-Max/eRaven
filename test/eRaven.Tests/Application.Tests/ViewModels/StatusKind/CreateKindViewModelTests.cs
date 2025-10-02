//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateKindViewModelTests
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Tests.Application.Tests.ViewModels.StatusKind;

public class CreateKindViewModelTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var ctx = new ValidationContext(model, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, ctx, results, validateAllProperties: true);
        return results;
    }

    private static string S(int len) => new('x', len);

    [Fact]
    public void Defaults_Are_Valid_For_Boolean_And_Order()
    {
        var vm = new CreateKindViewModel { Name = "Ok", Code = "OK" };

        var results = Validate(vm);

        Assert.Empty(results);               // валідна модель
        Assert.Equal(0, vm.Order);           // дефолт 0
        Assert.True(vm.IsActive);            // дефолт true
    }

    [Fact]
    public void Name_Required_Fails_On_Empty()
    {
        var vm = new CreateKindViewModel { Name = "", Code = "OK" };

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateKindViewModel.Name)));
    }

    [Fact]
    public void Code_Required_Fails_On_Empty()
    {
        var vm = new CreateKindViewModel { Name = "Ok", Code = "" };

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateKindViewModel.Code)));
    }

    [Fact]
    public void Name_MaxLength_128_Enforced()
    {
        var vmOk = new CreateKindViewModel { Name = S(128), Code = "OK" };
        var vmTooLong = new CreateKindViewModel { Name = S(129), Code = "OK" };

        Assert.Empty(Validate(vmOk));
        Assert.Contains(Validate(vmTooLong), r => r.MemberNames.Contains(nameof(CreateKindViewModel.Name)));
    }

    [Fact]
    public void Code_MaxLength_16_Enforced()
    {
        var vmOk = new CreateKindViewModel { Name = "Ok", Code = S(16) };
        var vmTooLong = new CreateKindViewModel { Name = "Ok", Code = S(17) };

        Assert.Empty(Validate(vmOk));
        Assert.Contains(Validate(vmTooLong), r => r.MemberNames.Contains(nameof(CreateKindViewModel.Code)));
    }

    [Theory]
    [InlineData("A", "C")]                 // мінімальні робочі значення
    [InlineData("Назва", "K1")]            // кирилиця/латиниця
    [InlineData("x", "xxxxxxxxxxxxxxxx")]  // рівно 16 символів у Code
    public void Valid_Cases_Pass(string name, string code)
    {
        var vm = new CreateKindViewModel { Name = name, Code = code };

        var results = Validate(vm);

        Assert.Empty(results);
    }
}