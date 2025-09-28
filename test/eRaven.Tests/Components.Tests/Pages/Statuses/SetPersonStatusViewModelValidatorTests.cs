//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SetPersonStatusViewModelValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PersonStatusViewModels;
using eRaven.Components.Pages.Statuses.Modals;
using FluentValidation.TestHelper;

namespace eRaven.Tests.Components.Tests.Pages.Statuses;

public sealed class SetPersonStatusViewModelValidatorTests
{
    private readonly SetPersonStatusViewModelValidator _validator = new();

    private static SetPersonStatusViewModel Valid() => new()
    {
        PersonId = Guid.NewGuid(),
        StatusId = 1,
        Moment = new DateTime(2025, 9, 1),
        Note = "ok",
        Author = "tester"
    };

    [Fact(DisplayName = "Validator: валідний VM проходить без помилок")]
    public void ValidModel_Passes()
    {
        var vm = Valid();
        var result = _validator.TestValidate(vm);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact(DisplayName = "Validator: PersonId обов'язковий (Guid.Empty → помилка)")]
    public void PersonId_IsRequired()
    {
        var vm = Valid();
        vm.PersonId = Guid.Empty;

        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.PersonId)
              .WithErrorMessage("Особа обов'язкова.");
    }

    [Fact(DisplayName = "Validator: StatusId > 0 (0 → помилка)")]
    public void StatusId_MustBePositive()
    {
        var vm = Valid();
        vm.StatusId = 0;

        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.StatusId)
              .WithErrorMessage("Статус обов'язковий.");
    }

    [Fact(DisplayName = "Validator: Moment не може бути default")]
    public void Moment_CannotBeDefault()
    {
        var vm = Valid();
        vm.Moment = default;

        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.Moment);
    }

    [Theory(DisplayName = "Validator: Moment у діапазоні 2000–2100")]
    [InlineData(1999, 12, 31)]
    [InlineData(2101, 1, 1)]
    public void Moment_MustBeWithinAllowedRange(int y, int m, int d)
    {
        var vm = Valid();
        vm.Moment = new DateTime(y, m, d);

        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.Moment)
              .WithErrorMessage("Дата виходить за допустимий діапазон (2000–2100).");
    }

    [Fact(DisplayName = "Validator: Note довша за 512 → помилка")]
    public void Note_MaxLength()
    {
        var vm = Valid();
        vm.Note = new string('x', 513);

        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.Note)
              .WithErrorMessage("Нотатка занадто довга (до 512).");
    }

    [Fact(DisplayName = "Validator: Note з одних пробілів → помилка")]
    public void Note_WhitespaceOnly_IsInvalid()
    {
        var vm = Valid();
        vm.Note = "   ";

        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.Note)
              .WithErrorMessage("Нотатка не може складатися лише з пробілів.");
    }

    [Fact(DisplayName = "Validator: Note = null → ок")]
    public void Note_Null_IsOk()
    {
        var vm = Valid();
        vm.Note = null;

        var result = _validator.TestValidate(vm);
        result.ShouldNotHaveValidationErrorFor(x => x.Note);
    }

    [Fact(DisplayName = "Validator: Author довший за 64 → помилка")]
    public void Author_MaxLength()
    {
        var vm = Valid();
        vm.Author = new string('y', 65);

        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.Author)
              .WithErrorMessage("Автор занадто довгий (до 64).");
    }

    [Fact(DisplayName = "Validator: Author з одних пробілів → помилка")]
    public void Author_WhitespaceOnly_IsInvalid()
    {
        var vm = Valid();
        vm.Author = "   ";

        var result = _validator.TestValidate(vm);
        result.ShouldHaveValidationErrorFor(x => x.Author)
              .WithErrorMessage("Автор не може складатися лише з пробілів.");
    }

    [Fact(DisplayName = "Validator: Author = null → ок")]
    public void Author_Null_IsOk()
    {
        var vm = Valid();
        vm.Author = null;

        var result = _validator.TestValidate(vm);
        result.ShouldNotHaveValidationErrorFor(x => x.Author);
    }
}
