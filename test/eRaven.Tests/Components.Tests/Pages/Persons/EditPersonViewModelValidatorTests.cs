//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EditPersonViewModelValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Components.Pages.Persons;            // де лежить EditPersonViewModelValidator
using FluentValidation.TestHelper;

namespace eRaven.Tests.Components.Tests.Pages.Persons;

public sealed class EditPersonViewModelValidatorTests
{
    private readonly EditPersonViewModelValidator _validator = new();

    // ================= helpers =================

    private static EditPersonViewModel Valid()
        => new()
        {
            LastName = "Петренко",
            FirstName = "Іван",
            MiddleName = "Іванович",
            Rnokpp = "1234567890",
            Rank = "сержант",
            BZVP = "пройшов",
            Weapon = "АК-74 №123",
            Callsign = "Сокіл"
        };

    // ================= happy path =================

    [Fact(DisplayName = "Valid: повністю валідна модель -> OK")]
    public void Valid_Model_Passes()
    {
        var m = Valid();
        var res = _validator.TestValidate(m);
        res.ShouldNotHaveAnyValidationErrors();
    }

    // ================= required + trim/whitespace =================

    [Theory(DisplayName = "Required: прізвище/ім’я порожні або з пробілів -> помилка")]
    [InlineData("", "Іван")]
    [InlineData("   ", "Іван")]
    [InlineData("Петренко", "")]
    [InlineData("Петренко", "   ")]
    public void Required_LastFirst_Errors(string last, string first)
    {
        var m = Valid();
        m.LastName = last;
        m.FirstName = first;

        var res = _validator.TestValidate(m);
        if (string.IsNullOrWhiteSpace(last))
            res.ShouldHaveValidationErrorFor(x => x.LastName);
        if (string.IsNullOrWhiteSpace(first))
            res.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Theory(DisplayName = "Rank: обовʼязкове, trim() → whitespace only -> помилка")]
    [InlineData("")]
    [InlineData("   ")]
    public void Rank_Required_WhitespaceOnly_Error(string rank)
    {
        var m = Valid(); m.Rank = rank;
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.Rank);
    }

    [Theory(DisplayName = "BZVP: обовʼязкове, trim() → whitespace only -> помилка")]
    [InlineData("")]
    [InlineData("   ")]
    public void BZVP_Required_WhitespaceOnly_Error(string bzvp)
    {
        var m = Valid(); m.BZVP = bzvp;
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.BZVP);
    }

    // ================= RNOKPP =================

    [Theory(DisplayName = "RNOKPP: повинен мати рівно 10 цифр -> невірна довжина/символи = помилка")]
    [InlineData("123456789")]     // 9
    [InlineData("12345678901")]   // 11
    [InlineData("123456789a")]    // літера
    [InlineData("abcdefghij")]    // все літери
    [InlineData("12345 6789")]    // пробіл усередині
    public void RNOKPP_Length_And_DigitsOnly_Error(string rnokpp)
    {
        var m = Valid(); m.Rnokpp = rnokpp;
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.Rnokpp);
    }

    [Fact(DisplayName = "RNOKPP: рівно 10 цифр -> OK")]
    public void RNOKPP_Exactly10Digits_Ok()
    {
        var m = Valid(); m.Rnokpp = "0123456789";
        var res = _validator.TestValidate(m);
        res.ShouldNotHaveValidationErrorFor(x => x.Rnokpp);
    }

    // ================= MiddleName =================

    [Fact(DisplayName = "MiddleName: null/empty -> OK (опційне)")]
    public void MiddleName_NullOrEmpty_Ok()
    {
        var m1 = Valid(); m1.MiddleName = null;
        var r1 = _validator.TestValidate(m1);
        r1.ShouldNotHaveValidationErrorFor(x => x.MiddleName);

        var m2 = Valid(); m2.MiddleName = "";
        var r2 = _validator.TestValidate(m2);
        r2.ShouldNotHaveValidationErrorFor(x => x.MiddleName);
    }

    [Fact(DisplayName = "MiddleName: лише пробіли -> помилка (не допускаємо whitespaces-only)")]
    public void MiddleName_WhitespacesOnly_Error()
    {
        var m = Valid(); m.MiddleName = "   ";
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.MiddleName);
    }

    // ================= MaxLength constraints =================

    [Fact(DisplayName = "MaxLength: Weapon >128 символів -> помилка")]
    public void Weapon_MaxLength_Error()
    {
        var m = Valid(); m.Weapon = new string('W', 129);
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.Weapon);
    }

    [Fact(DisplayName = "MaxLength: Callsign >64 символів -> помилка")]
    public void Callsign_MaxLength_Error()
    {
        var m = Valid(); m.Callsign = new string('C', 65);
        var res = _validator.TestValidate(m);
        res.ShouldHaveValidationErrorFor(x => x.Callsign);
    }

    [Fact(DisplayName = "MaxLength: Last/First/Middle/Rank/BZVP з граничними довжинами -> OK")]
    public void MaxLength_Boundaries_Ok()
    {
        var m = Valid();
        m.LastName = new string('L', 128);
        m.FirstName = new string('F', 128);
        m.MiddleName = new string('M', 128);
        m.Rank = new string('R', 64);
        m.BZVP = new string('B', 128);

        var res = _validator.TestValidate(m);
        res.ShouldNotHaveAnyValidationErrors();
    }

    // ================= Trimming semantics =================

    [Fact(DisplayName = "Trim: поля з пробілами з країв проходять (валідатор перевіряє зміст)")]
    public void Trimmed_Values_Accepted()
    {
        var m = Valid();
        m.LastName = "  Петренко  ";
        m.FirstName = "  Іван  ";
        m.Rank = "  сержант ";
        m.BZVP = "  пройшов ";

        var res = _validator.TestValidate(m);
        res.ShouldNotHaveAnyValidationErrors();
    }

    // ================= Aggregate (декілька помилок одразу) =================

    [Fact(DisplayName = "Multiple errors: порожні/некоректні поля -> кілька помилок")]
    public void Multiple_Errors_Aggregated()
    {
        var m = new EditPersonViewModel
        {
            LastName = " ",
            FirstName = "",
            MiddleName = "   ",
            Rnokpp = "12345",
            Rank = " ",
            BZVP = ""
        };

        var res = _validator.TestValidate(m);

        res.ShouldHaveValidationErrorFor(x => x.LastName);
        res.ShouldHaveValidationErrorFor(x => x.FirstName);
        res.ShouldHaveValidationErrorFor(x => x.MiddleName);
        res.ShouldHaveValidationErrorFor(x => x.Rnokpp);
        res.ShouldHaveValidationErrorFor(x => x.Rank);
        res.ShouldHaveValidationErrorFor(x => x.BZVP);
    }
}
