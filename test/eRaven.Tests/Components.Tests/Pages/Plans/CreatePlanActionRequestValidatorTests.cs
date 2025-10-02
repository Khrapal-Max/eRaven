//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanActionRequestValidatorTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Components.Tests.Pages.Plans;

public class CreatePlanActionRequestValidatorTests
{
    private static PlanAction ValidModel()
    {
        return new PlanAction
        {
            Id = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),

            PlanActionName = "R-001/25",
            EffectiveAtUtc = DateTime.SpecifyKind(new DateTime(2025, 9, 21, 12, 0, 0), DateTimeKind.Utc),
            ToStatusKindId = 10,
            Order = null, // не використовується при створенні

            ActionState = ActionState.PlanAction,
            MoveType = MoveType.Dispatch,
            Location = "Sector A",
            GroupName = "Alpha",
            CrewName = "Crew-1",
            Note = "Initial note",

            Rnokpp = "1234567890",
            FullName = "Тестовий Користувач",
            RankName = "Сержант",
            PositionName = "Відділення звʼязку",
            BZVP = "БЗ-42",
            Weapon = "АК-74",
            Callsign = "Сокіл",
            StatusKindOnDate = "В строю 21.09.2025 10:30"
        };
    }

    [Fact]
    public void Validate_ValidModel_Should_Pass()
    {
        // Arrange
        var model = ValidModel();
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Fact]
    public void PersonId_Empty_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.PersonId = Guid.Empty;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.PersonId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("R-001")] // ок, довжина < 128
    [InlineData("")]      // порожній рядок — за правилом Must (не лише пробіли) це НЕ ОК
    [InlineData("   ")]   // тільки пробіли — НЕ ОК
    public void PlanActionName_Rules(string? value)
    {
        // Arrange
        var model = ValidModel();
        model.PlanActionName = value!;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var result = validator.Validate(model);

        // Assert
        if (value is null)
        {
            // MaximumLength допускає null; Must дозволяє null → валідно
            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }
        else if (string.IsNullOrWhiteSpace(value))
        {
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.PlanActionName));
        }
        else
        {
            Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
        }
    }

    [Fact]
    public void PlanActionName_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.PlanActionName = new string('X', 129);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var result = validator.Validate(model);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(model.PlanActionName));
    }

    [Fact]
    public void EffectiveAtUtc_Default_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.EffectiveAtUtc = default;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.EffectiveAtUtc));
    }

    [Fact]
    public void EffectiveAtUtc_NotUtc_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.EffectiveAtUtc = new DateTime(2025, 9, 21, 12, 0, 0, DateTimeKind.Local);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.EffectiveAtUtc));
    }

    [Fact]
    public void EffectiveAtUtc_OutOfRange_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.EffectiveAtUtc = DateTime.SpecifyKind(new DateTime(1999, 12, 31), DateTimeKind.Utc);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.EffectiveAtUtc));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ToStatusKindId_MustBe_GreaterThanZero(int value)
    {
        // Arrange
        var model = ValidModel();
        model.ToStatusKindId = value;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.ToStatusKindId));
    }

    [Fact]
    public void MoveType_Invalid_Enum_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.MoveType = (MoveType)999;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.MoveType));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Location_Required_Should_Fail_On_EmptyOrWhitespace(string? value)
    {
        // Arrange
        var model = ValidModel();
        model.Location = value ?? string.Empty;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.Location));
    }

    [Fact]
    public void Location_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.Location = new string('L', 257);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.Location));
    }

    [Theory]
    [InlineData("   ")]
    public void GroupName_WhitespaceOnly_Should_Fail(string value)
    {
        // Arrange
        var model = ValidModel();
        model.GroupName = value;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.GroupName));
    }

    [Fact]
    public void GroupName_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.GroupName = new string('G', 129);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.GroupName));
    }

    [Theory]
    [InlineData("   ")]
    public void CrewName_WhitespaceOnly_Should_Fail(string value)
    {
        // Arrange
        var model = ValidModel();
        model.CrewName = value;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.CrewName));
    }

    [Fact]
    public void CrewName_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.CrewName = new string('C', 129);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.CrewName));
    }

    [Theory]
    [InlineData("   ")]
    public void Note_WhitespaceOnly_Should_Fail(string value)
    {
        // Arrange
        var model = ValidModel();
        model.Note = value;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.Note));
    }

    [Fact]
    public void Note_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.Note = new string('N', 513);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.Note));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Rnokpp_Required_Should_Fail_On_EmptyOrWhitespace(string? value)
    {
        // Arrange
        var model = ValidModel();
        model.Rnokpp = value ?? string.Empty;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.Rnokpp));
    }

    [Fact]
    public void Rnokpp_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.Rnokpp = new string('R', 17);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.Rnokpp));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FullName_Required_Should_Fail_On_EmptyOrWhitespace(string? value)
    {
        // Arrange
        var model = ValidModel();
        model.FullName = value ?? string.Empty;
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.FullName));
    }

    [Fact]
    public void FullName_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.FullName = new string('F', 257);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.FullName));
    }

    [Fact]
    public void RankName_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.RankName = new string('K', 65);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.RankName));
    }

    [Fact]
    public void PositionName_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.PositionName = new string('P', 129);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.PositionName));
    }

    [Fact]
    public void BZVP_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.BZVP = new string('B', 129);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.BZVP));
    }

    [Fact]
    public void Weapon_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.Weapon = new string('W', 129);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.Weapon));
    }

    [Fact]
    public void Callsign_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.Callsign = new string('C', 129);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.Callsign));
    }

    [Fact]
    public void StatusKindOnDate_TooLong_Should_Fail()
    {
        // Arrange
        var model = ValidModel();
        model.StatusKindOnDate = new string('S', 65);
        var validator = new CreatePlanActionRequestValidator();

        // Act
        var res = validator.Validate(model);

        // Assert
        Assert.False(res.IsValid);
        Assert.Contains(res.Errors, e => e.PropertyName == nameof(model.StatusKindOnDate));
    }
}
