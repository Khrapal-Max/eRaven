//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateKindViewModelValidatorTests
//-----------------------------------------------------------------------------

using FluentValidation.TestHelper;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.StatusTransitions;

public class CreateKindViewModelValidatorTests
{
    private static CreateKindViewModel ValidModel() => new()
    {
        Name = "Новий статус",
        Code = "NEW",
        Order = 0,
        IsActive = true
    };

    private static (CreateKindViewModelValidator validator, Mock<IStatusKindService> svc) MakeValidator()
    {
        var svc = new Mock<IStatusKindService>(MockBehavior.Strict);

        // ДЕФОЛТ: унікальні (нема дублів)
        svc.Setup(s => s.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(false);
        svc.Setup(s => s.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
           .ReturnsAsync(false);

        var validator = new CreateKindViewModelValidator(svc.Object);
        return (validator, svc);
    }

    [Fact]
    public async Task Valid_Model_Passes()
    {
        var (validator, svc) = MakeValidator();

        var result = await validator.TestValidateAsync(ValidModel());
        result.ShouldNotHaveAnyValidationErrors();

        svc.Verify(s => s.NameExistsAsync("Новий статус", It.IsAny<CancellationToken>()), Times.Once);
        svc.Verify(s => s.CodeExistsAsync("NEW", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Name_Empty_Fails_And_DoesNotCall_NameExists()
    {
        var (validator, svc) = MakeValidator();

        var m = ValidModel();
        m.Name = "";

        var r = await validator.TestValidateAsync(m);
        r.ShouldHaveValidationErrorFor(x => x.Name);

        svc.Verify(s => s.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Name_WhitespaceOnly_Fails_And_DoesNotCall_NameExists()
    {
        var (validator, svc) = MakeValidator();

        var m = ValidModel();
        m.Name = "   ";

        var r = await validator.TestValidateAsync(m);
        r.ShouldHaveValidationErrorFor(x => x.Name);

        svc.Verify(s => s.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Name_TooShort_Fails_And_DoesNotCall_NameExists()
    {
        var (validator, svc) = MakeValidator();

        var m = ValidModel();
        m.Name = "A";

        var r = await validator.TestValidateAsync(m);
        r.ShouldHaveValidationErrorFor(x => x.Name);

        svc.Verify(s => s.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Name_TooLong_Fails_And_DoesNotCall_NameExists()
    {
        var (validator, svc) = MakeValidator();

        var m = ValidModel();
        m.Name = new string('N', 129);

        var r = await validator.TestValidateAsync(m);
        r.ShouldHaveValidationErrorFor(x => x.Name);

        svc.Verify(s => s.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Name_Duplicate_By_Service_Fails()
    {
        var (validator, svc) = MakeValidator();

        // Емулюємо дублікат Name
        svc.Setup(s => s.NameExistsAsync("Новий статус", It.IsAny<CancellationToken>()))
           .ReturnsAsync(true);

        var r = await validator.TestValidateAsync(ValidModel());

        r.ShouldHaveValidationErrorFor(x => x.Name)
         .WithErrorMessage("Статус з такою назвою вже існує.");

        svc.Verify(s => s.NameExistsAsync("Новий статус", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Code_Empty_Fails_And_DoesNotCall_CodeExists()
    {
        var (validator, svc) = MakeValidator();

        var m = ValidModel();
        m.Code = "";

        var r = await validator.TestValidateAsync(m);
        r.ShouldHaveValidationErrorFor(x => x.Code);

        svc.Verify(s => s.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        // name при цьому валідний, тож його унікальність могла викликатися — це ок
    }

    [Fact]
    public async Task Code_WhitespaceOnly_Fails_And_DoesNotCall_CodeExists()
    {
        var (validator, svc) = MakeValidator();

        var m = ValidModel();
        m.Code = "   ";

        var r = await validator.TestValidateAsync(m);
        r.ShouldHaveValidationErrorFor(x => x.Code);

        svc.Verify(s => s.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Code_TooLong_Fails_And_DoesNotCall_CodeExists()
    {
        var (validator, svc) = MakeValidator();

        var m = ValidModel();
        m.Code = new string('C', 17);

        var r = await validator.TestValidateAsync(m);
        r.ShouldHaveValidationErrorFor(x => x.Code);

        svc.Verify(s => s.CodeExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Code_Duplicate_By_Service_Fails()
    {
        var (validator, svc) = MakeValidator();

        // Емулюємо дублікат Code
        svc.Setup(s => s.CodeExistsAsync("NEW", It.IsAny<CancellationToken>()))
           .ReturnsAsync(true);

        var r = await validator.TestValidateAsync(ValidModel());

        r.ShouldHaveValidationErrorFor(x => x.Code)
         .WithErrorMessage("Код вже використовується.");

        svc.Verify(s => s.CodeExistsAsync("NEW", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Order_Negative_Fails()
    {
        var (validator, svc) = MakeValidator();

        var m = ValidModel();
        m.Order = -1;

        var r = await validator.TestValidateAsync(m);
        r.ShouldHaveValidationErrorFor(x => x.Order);
    }

    [Fact]
    public async Task Name_And_Code_Are_Trimmed_For_Service_Checks()
    {
        var (validator, svc) = MakeValidator();

        var m = new CreateKindViewModel
        {
            Name = "  Статус  ",
            Code = "  X1  ",
            Order = 0,
            IsActive = true
        };

        var r = await validator.TestValidateAsync(m);
        r.ShouldNotHaveAnyValidationErrors();

        svc.Verify(s => s.NameExistsAsync("Статус", It.IsAny<CancellationToken>()), Times.Once);
        svc.Verify(s => s.CodeExistsAsync("X1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
