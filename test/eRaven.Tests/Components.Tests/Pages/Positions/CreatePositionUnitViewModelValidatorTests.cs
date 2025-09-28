//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePositionUnitViewModelValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PositionService;
using eRaven.Application.ViewModels.PositionPagesViewModels;
using eRaven.Components.Pages.Positions.Modals;
using FluentValidation.TestHelper;
using Moq;

namespace eRaven.Tests.Components.Tests.Pages.Positions;

public class CreatePositionUnitViewModelValidatorTests
{
    private static CreatePositionUnitViewModel NewModel(
        string code = "A1",
        string shortName = "Позиція",
        string specialNumber = "12-345",
        string orgPath = "Шлях / Організація") => new()
        {
            Code = code,
            ShortName = shortName,
            SpecialNumber = specialNumber,
            OrgPath = orgPath
        };

    [Fact]
    public async Task Valid_Model_Passes()
    {
        // Arrange
        var svc = new Mock<IPositionService>();
        svc.Setup(s => s.CodeExistsActiveAsync("A1", It.IsAny<CancellationToken>()))
           .ReturnsAsync(false);

        var sut = new CreatePositionUnitViewModelValidator(svc.Object);
        var model = NewModel();

        // Act
        var res = await sut.TestValidateAsync(model);

        // Assert
        res.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Code_Is_Required(string? bad)
    {
        var svc = new Mock<IPositionService>();
        var sut = new CreatePositionUnitViewModelValidator(svc.Object);
        var model = NewModel(code: bad!);

        var res = await sut.TestValidateAsync(model);

        res.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public async Task Code_Whitespace_Only_Fails()
    {
        var svc = new Mock<IPositionService>();
        var sut = new CreatePositionUnitViewModelValidator(svc.Object);
        var model = NewModel(code: "   ");

        var res = await sut.TestValidateAsync(model);

        res.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public async Task Code_Duplicate_Among_Active_Fails()
    {
        // Arrange
        var svc = new Mock<IPositionService>();
        svc.Setup(s => s.CodeExistsActiveAsync("DUP", It.IsAny<CancellationToken>()))
           .ReturnsAsync(true);

        var sut = new CreatePositionUnitViewModelValidator(svc.Object);
        var model = NewModel(code: "DUP");

        // Act
        var res = await sut.TestValidateAsync(model);

        // Assert
        res.ShouldHaveValidationErrorFor(x => x.Code)
           .WithErrorMessage("Активна посада з таким кодом вже існує.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ShortName_Required(string? bad)
    {
        var svc = new Mock<IPositionService>();
        var sut = new CreatePositionUnitViewModelValidator(svc.Object);
        var model = NewModel(shortName: bad!);

        var res = await sut.TestValidateAsync(model);

        res.ShouldHaveValidationErrorFor(x => x.ShortName);
    }

    [Fact]
    public async Task SpecialNumber_Required_And_MaxLength()
    {
        var svc = new Mock<IPositionService>();
        var sut = new CreatePositionUnitViewModelValidator(svc.Object);

        var missing = NewModel(specialNumber: "");
        var res1 = await sut.TestValidateAsync(missing);
        res1.ShouldHaveValidationErrorFor(x => x.SpecialNumber);

        var longStr = new string('X', 16);
        var tooLong = NewModel(specialNumber: longStr);
        var res2 = await sut.TestValidateAsync(tooLong);
        res2.ShouldHaveValidationErrorFor(x => x.SpecialNumber);
    }

    [Fact]
    public async Task OrgPath_Required_And_MaxLength()
    {
        var svc = new Mock<IPositionService>();
        var sut = new CreatePositionUnitViewModelValidator(svc.Object);

        var missing = NewModel(orgPath: "");
        var res1 = await sut.TestValidateAsync(missing);
        res1.ShouldHaveValidationErrorFor(x => x.OrgPath);

        var longStr = new string('Y', 513);
        var tooLong = NewModel(orgPath: longStr);
        var res2 = await sut.TestValidateAsync(tooLong);
        res2.ShouldHaveValidationErrorFor(x => x.OrgPath);
    }
}
