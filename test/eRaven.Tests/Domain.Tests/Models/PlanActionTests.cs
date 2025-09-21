//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Tests.Domain.Tests.Models.Helpers;
using System.Text.Json;

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanActionTests
{
    [Fact]
    public void Default_ActionState_ShouldBe_PlanAction()
    {
        // Arrange
        var pa = new PlanAction();

        // Act
        var state = pa.ActionState;

        // Assert
        Assert.Equal(ActionState.PlanAction, state);
    }

    [Fact]
    public void Create_ValidPlanAction_ShouldHoldAssignedValues()
    {
        // Arrange
        var id = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var effective = new DateTime(2025, 9, 21, 8, 30, 0, DateTimeKind.Utc);
        var pa = PlanActionFactory.CreateValid(id, personId, MoveType.Dispatch, effective);

        // Act
        // (нічого не робимо, просто читаємо властивості)

        // Assert
        Assert.Equal(id, pa.Id);
        Assert.Equal(personId, pa.PersonId);

        Assert.Equal("R-001/24", pa.PlanActionName);
        Assert.Equal(effective, pa.EffectiveAtUtc);
        Assert.Equal(1, pa.ToStatusKindId);
        Assert.Null(pa.Order);
        Assert.Equal(ActionState.PlanAction, pa.ActionState);

        Assert.Equal(MoveType.Dispatch, pa.MoveType);
        Assert.Equal("Сектор Б", pa.Location);
        Assert.Equal("Група Альфа", pa.GroupName);
        Assert.Equal("Екіпаж 1", pa.CrewName);
        Assert.Equal("Попередня розвідка", pa.Note);

        Assert.Equal("1234567890", pa.Rnokpp);
        Assert.Equal("Іваненко Іван Іванович", pa.FullName);
        Assert.Equal("Сержант", pa.RankName);
        Assert.Equal("Відділення Звʼязку", pa.PositionName);
        Assert.Equal("БЗ-42", pa.BZVP);
        Assert.Equal("АК-74", pa.Weapon);
        Assert.Equal("Сокіл", pa.Callsign);
        Assert.Equal("В строю 21.09.2025 10:30", pa.StatusKindOnDate);
    }

    [Fact]
    public void Approve_Order_ShouldSetApprovedState_AndOrder()
    {
        // Arrange
        var pa = PlanActionFactory.CreateValid(move: MoveType.Return);

        // Act
        pa.ActionState = ActionState.ApprovedOrder;
        pa.Order = "БР-77/25";

        // Assert
        Assert.Equal(ActionState.ApprovedOrder, pa.ActionState);
        Assert.Equal("БР-77/25", pa.Order);
    }

    [Fact]
    public void MoveType_ShouldBeDefinedEnum()
    {
        // Arrange
        var pa = PlanActionFactory.CreateValid(move: MoveType.Dispatch);

        // Act
        var isDefined = Enum.IsDefined(pa.MoveType);

        // Assert
        Assert.True(isDefined);
    }

    [Fact]
    public void EffectiveAtUtc_ShouldKeepUtcKind()
    {
        // Arrange
        var utc = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var pa = PlanActionFactory.CreateValid(effectiveUtc: utc);

        // Act
        var kind = pa.EffectiveAtUtc.Kind;

        // Assert
        Assert.Equal(DateTimeKind.Utc, kind);
    }

    [Fact]
    public void Json_SerializeDeserialize_ShouldRoundTrip()
    {
        // Arrange
        var source = PlanActionFactory.CreateValid();
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Act
        var json = JsonSerializer.Serialize(source, options);
        var restored = JsonSerializer.Deserialize<PlanAction>(json, options);

        // Assert
        Assert.NotNull(restored);
        Assert.Equal(source.Id, restored!.Id);
        Assert.Equal(source.PersonId, restored.PersonId);
        Assert.Equal(source.PlanActionName, restored.PlanActionName);
        Assert.Equal(source.EffectiveAtUtc, restored.EffectiveAtUtc);
        Assert.Equal(source.ToStatusKindId, restored.ToStatusKindId);
        Assert.Equal(source.Order, restored.Order);
        Assert.Equal(source.ActionState, restored.ActionState);
        Assert.Equal(source.MoveType, restored.MoveType);
        Assert.Equal(source.Location, restored.Location);
        Assert.Equal(source.GroupName, restored.GroupName);
        Assert.Equal(source.CrewName, restored.CrewName);
        Assert.Equal(source.Note, restored.Note);
        Assert.Equal(source.Rnokpp, restored.Rnokpp);
        Assert.Equal(source.FullName, restored.FullName);
        Assert.Equal(source.RankName, restored.RankName);
        Assert.Equal(source.PositionName, restored.PositionName);
        Assert.Equal(source.BZVP, restored.BZVP);
        Assert.Equal(source.Weapon, restored.Weapon);
        Assert.Equal(source.Callsign, restored.Callsign);
        Assert.Equal(source.StatusKindOnDate, restored.StatusKindOnDate);
    }

    [Fact]
    public void Can_Set_ToStatusKindId_Null_AfterApprove()
    {
        // Arrange
        var pa = PlanActionFactory.CreateValid();
        Assert.NotNull(pa.ToStatusKindId);

        // Act
        pa.ActionState = ActionState.ApprovedOrder;
        pa.ToStatusKindId = null;

        // Assert
        Assert.Null(pa.ToStatusKindId);
    }
}
