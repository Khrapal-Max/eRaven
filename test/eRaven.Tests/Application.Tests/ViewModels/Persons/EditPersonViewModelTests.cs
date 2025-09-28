//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EditPersonViewModelTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace eRaven.Tests.Application.Tests.ViewModels.Persons;

public class EditPersonViewModelTests
{
    // ----------------- Mapping -----------------

    [Fact(DisplayName = "From(Person): мапить усі поля 1:1")]
    public void From_Maps_All_Fields()
    {
        var p = new Person
        {
            LastName = "Петренко",
            FirstName = "Петро",
            MiddleName = "Петрович",
            Rnokpp = "1234567890",
            Rank = "сержант",
            BZVP = "потребує навчання",
            Weapon = "АК-74 №123",
            Callsign = "Сокіл"
        };

        var vm = EditPersonViewModel.From(p);

        Assert.Equal(p.LastName, vm.LastName);
        Assert.Equal(p.FirstName, vm.FirstName);
        Assert.Equal(p.MiddleName, vm.MiddleName);
        Assert.Equal(p.Rnokpp, vm.Rnokpp);
        Assert.Equal(p.Rank, vm.Rank);
        Assert.Equal(p.BZVP, vm.BZVP);
        Assert.Equal(p.Weapon, vm.Weapon);
        Assert.Equal(p.Callsign, vm.Callsign);
    }

    // ----------------- Validation: Required -----------------

    [Fact(DisplayName = "Validation: порожні обовʼязкові поля -> помилки")]
    public void Validation_Required_Fields()
    {
        var vm = new EditPersonViewModel
        {
            LastName = "",             // required
            FirstName = "",            // required
            MiddleName = null,         // optional
            Rnokpp = "",               // required (10 символів)
            Rank = "",                 // required (після фікса додавання [Required])
            BZVP = "",                 // required
            Weapon = null,             // optional
            Callsign = null            // optional
        };

        var results = Validate(vm);

        // Перевіряємо, що є помилки по required полях
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.LastName)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.FirstName)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.Rnokpp)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.Rank)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.BZVP)));
    }

    // ----------------- Validation: MaxLength & формат -----------------

    [Fact(DisplayName = "Validation: RNOKPP не 10 символів -> помилка")]
    public void Validation_Rnokpp_Length()
    {
        var vm = Valid();
        vm.Rnokpp = "12345";

        var results = Validate(vm);

        Assert.Contains(results, x => x.ErrorMessage!.Contains("рівно 10"));
    }

    [Fact(DisplayName = "Validation: перевищення MaxLength -> помилки")]
    public void Validation_MaxLength()
    {
        var vm = Valid();

        vm.LastName = new string('x', 129); // 128 max
        vm.FirstName = new string('x', 129); // 128 max
        vm.MiddleName = new string('x', 129); // 128 max (optional)
        vm.Rank = new string('x', 65); // 64 max
        vm.BZVP = new string('x', 129); // 128 max
        vm.Weapon = new string('x', 129); // 128 max
        vm.Callsign = new string('x', 65); // 64 max

        var results = Validate(vm);

        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.LastName)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.FirstName)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.MiddleName)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.Rank)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.BZVP)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.Weapon)));
        Assert.Contains(results, e => e.MemberNames.Contains(nameof(EditPersonViewModel.Callsign)));
    }

    [Fact(DisplayName = "Validation: валідна модель -> без помилок")]
    public void Validation_Valid_Passes()
    {
        var vm = Valid();
        var results = Validate(vm);
        Assert.Empty(results);
    }

    // ----------------- Helpers -----------------

    private static List<ValidationResult> Validate(object vm)
    {
        var ctx = new ValidationContext(vm, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(vm, ctx, results, validateAllProperties: true);
        return results;
    }

    private static EditPersonViewModel Valid() => new()
    {
        LastName = "Петренко",
        FirstName = "Петро",
        MiddleName = "Петрович",
        Rnokpp = "1234567890",   // 10 символів
        Rank = "сержант",
        BZVP = "потребує навчання",
        Weapon = "АК-74 №123",
        Callsign = "Сокіл"
    };
}