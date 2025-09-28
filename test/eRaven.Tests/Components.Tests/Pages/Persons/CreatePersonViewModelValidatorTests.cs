//-----------------------------------------------------------------------------
// Components/Pages/Persons/Modals/PersonCreateModal.razor.cs
//-----------------------------------------------------------------------------
// CreatePersonViewModelValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Components.Pages.Persons.Modals;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public class CreatePersonViewModelValidatorTests
{
    private readonly CreatePersonViewModelValidator _validator = new();

    private static CreatePersonViewModel ValidModel() => new()
    {
        LastName = "Шевченко",
        FirstName = "Тарас",
        MiddleName = "Григорович",
        Rnokpp = "1234567890",
        Rank = "сержант",
        BZVP = "потребує навчання",
        Weapon = "АК-74 №123",
        Callsign = "Кобзар"
    };

    // ----- Success case -----
    [Fact(DisplayName = "Valid model -> no errors")]
    public void ValidModel_NoErrors()
    {
        var m = ValidModel();
        var res = _validator.TestValidate(m);
        res.ShouldNotHaveAnyValidationErrors();
    }

    // ----- LastName -----
    [Theory(DisplayName = "LastName required and trimmed (no whitespaces only)")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void LastName_Required(string? val)
    {
        var m = ValidModel(); m.LastName = val!;
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact(DisplayName = "LastName min length 2")]
    public void LastName_MinLen()
    {
        var m = ValidModel(); m.LastName = "А";
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact(DisplayName = "LastName max length 128")]
    public void LastName_MaxLen()
    {
        var m = ValidModel(); m.LastName = new string('a', 129);
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    // ----- FirstName -----
    [Theory(DisplayName = "FirstName required and not whitespaces")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FirstName_Required(string? val)
    {
        var m = ValidModel(); m.FirstName = val!;
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact(DisplayName = "FirstName min length 2")]
    public void FirstName_MinLen()
    {
        var m = ValidModel(); m.FirstName = "І";
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact(DisplayName = "FirstName max length 128")]
    public void FirstName_MaxLen()
    {
        var m = ValidModel(); m.FirstName = new string('b', 129);
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    // ----- MiddleName (optional, but not whitespaces) -----
    [Fact(DisplayName = "MiddleName optional: null is OK")]
    public void MiddleName_Null_Ok()
    {
        var m = ValidModel(); m.MiddleName = null;
        var res = _validator.TestValidate(m);
        res.ShouldNotHaveValidationErrorFor(x => x.MiddleName);
    }

    [Fact(DisplayName = "MiddleName: whitespaces only -> error")]
    public void MiddleName_Whitespaces_Error()
    {
        var m = ValidModel(); m.MiddleName = "   ";
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.MiddleName);
    }

    [Fact(DisplayName = "MiddleName max length 128")]
    public void MiddleName_MaxLen()
    {
        var m = ValidModel(); m.MiddleName = new string('c', 129);
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.MiddleName);
    }

    // ----- RNOKPP (10 digits) -----
    [Theory(DisplayName = "Rnokpp required, 10 digits")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("abcdefghij")]
    [InlineData("123456789")]    // 9
    [InlineData("12345678901")]  // 11
    public void Rnokpp_Format(string? val)
    {
        var m = ValidModel(); m.Rnokpp = val!;
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.Rnokpp);
    }

    [Fact(DisplayName = "Rnokpp exactly 10 digits -> OK")]
    public void Rnokpp_10Digits_Ok()
    {
        var m = ValidModel(); m.Rnokpp = "0987654321";
        var res = _validator.TestValidate(m);
        res.ShouldNotHaveValidationErrorFor(x => x.Rnokpp);
    }

    // ----- Rank (required, not whitespaces) -----
    [Theory(DisplayName = "Rank required and not whitespaces")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rank_Required(string? val)
    {
        var m = ValidModel(); m.Rank = val!;
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.Rank);
    }

    [Fact(DisplayName = "Rank max length 64")]
    public void Rank_MaxLen()
    {
        var m = ValidModel(); m.Rank = new string('r', 65);
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.Rank);
    }

    // ----- BZVP (required, free text) -----
    [Theory(DisplayName = "BZVP required and not whitespaces")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bzvp_Required(string? val)
    {
        var m = ValidModel(); m.BZVP = val!;
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.BZVP);
    }

    [Fact(DisplayName = "BZVP max length 128")]
    public void Bzvp_MaxLen()
    {
        var m = ValidModel(); m.BZVP = new string('z', 129);
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.BZVP);
    }

    // ----- Weapon (optional, max 128) -----
    [Fact(DisplayName = "Weapon optional: null OK; >128 -> error")]
    public void Weapon_Optional_MaxLen()
    {
        var m1 = ValidModel(); m1.Weapon = null;
        _validator.TestValidate(m1).ShouldNotHaveValidationErrorFor(x => x.Weapon);

        var m2 = ValidModel(); m2.Weapon = new string('w', 129);
        _validator.TestValidate(m2).ShouldHaveValidationErrorFor(x => x.Weapon);
    }

    // ----- Callsign (optional, max 64) -----
    [Fact(DisplayName = "Callsign optional: null OK; >64 -> error")]
    public void Callsign_Optional_MaxLen()
    {
        var m1 = ValidModel(); m1.Callsign = null;
        _validator.TestValidate(m1).ShouldNotHaveValidationErrorFor(x => x.Callsign);

        var m2 = ValidModel(); m2.Callsign = new string('c', 65);
        _validator.TestValidate(m2).ShouldHaveValidationErrorFor(x => x.Callsign);
    }
}
