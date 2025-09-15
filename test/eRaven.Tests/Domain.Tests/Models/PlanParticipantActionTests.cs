//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanParticipantActionTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanParticipantActionTests
{
    [Fact]
    public void Ctor_Initializes_RecordedUtc_CloseToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-2);
        var action = new PlanParticipantAction();
        var after = DateTime.UtcNow.AddSeconds(2);

        Assert.True(action.RecordedUtc >= before && action.RecordedUtc <= after,
            $"RecordedUtc {action.RecordedUtc:o} is not within expected range [{before:o}..{after:o}]");
    }

    [Fact]
    public void Can_Set_And_Get_Identifiers()
    {
        var id = Guid.NewGuid();
        var pid = Guid.NewGuid(); // PlanParticipantId
        var planId = Guid.NewGuid();
        var personId = Guid.NewGuid();

        var action = new PlanParticipantAction
        {
            Id = id,
            PlanParticipantId = pid,
            PlanId = planId,
            PersonId = personId
        };

        Assert.Equal(id, action.Id);
        Assert.Equal(pid, action.PlanParticipantId);
        Assert.Equal(planId, action.PlanId);
        Assert.Equal(personId, action.PersonId);
    }

    [Fact]
    public void Can_Set_And_Get_ActionCore()
    {
        var when = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);

        var action = new PlanParticipantAction
        {
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = when
        };

        Assert.Equal(PlanActionType.Dispatch, action.ActionType);
        Assert.Equal(when, action.EventAtUtc);
        Assert.Equal(DateTimeKind.Utc, action.EventAtUtc.Kind);
    }

    [Fact]
    public void Can_Set_And_Get_ContextFields()
    {
        var action = new PlanParticipantAction
        {
            Location = "Локація-А",
            GroupName = "Група-1",
            CrewName = "Екіпаж-А",
            Note = "примітка",
            Author = "tester"
        };

        Assert.Equal("Локація-А", action.Location);
        Assert.Equal("Група-1", action.GroupName);
        Assert.Equal("Екіпаж-А", action.CrewName);
        Assert.Equal("примітка", action.Note);
        Assert.Equal("tester", action.Author);
    }

    [Fact]
    public void Can_Set_And_Get_Navigation_PlanParticipant()
    {
        var pp = new PlanParticipant
        {
            Id = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            FullName = "Петренко Петро Петрович",
            RankName = "Сержант",
            PositionName = "Стрілець",
            UnitName = "Взвод 1 / Рота 2"
        };

        var action = new PlanParticipantAction
        {
            PlanParticipant = pp
        };

        Assert.Same(pp, action.PlanParticipant);
    }

    [Fact]
    public void Defaults_Are_Uninitialized_For_Required_Strings()
    {
        // Зауваження: у домені поля позначені як non-nullable з `= default!;`
        // Це означає, що без встановлення вони фактично будуть null у рантаймі (до мапінгу/валідації).
        var action = new PlanParticipantAction();

        Assert.Null(action.Location);
        Assert.Null(action.GroupName);
        Assert.Null(action.CrewName);
        Assert.Null(action.Note);
        Assert.Null(action.Author);
    }
}
