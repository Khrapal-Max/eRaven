//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanParticipantTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanParticipantTests
{
    [Fact]
    public void Ctor_Initializes_Actions_AsEmptyCollection()
    {
        var pp = new PlanParticipant();

        Assert.NotNull(pp.Actions);
        Assert.Empty(pp.Actions);
    }

    [Fact]
    public void Ctor_Initializes_RecordedUtc_CloseToNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-2);
        var pp = new PlanParticipant();
        var after = DateTime.UtcNow.AddSeconds(2);

        Assert.True(pp.RecordedUtc >= before && pp.RecordedUtc <= after,
            $"RecordedUtc {pp.RecordedUtc:o} is not within expected range [{before:o}..{after:o}]");
    }

    [Fact]
    public void Can_Set_And_Get_Scalar_Identifiers()
    {
        var planId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var id = Guid.NewGuid();

        var pp = new PlanParticipant
        {
            Id = id,
            PlanId = planId,
            PersonId = personId
        };

        Assert.Equal(id, pp.Id);
        Assert.Equal(planId, pp.PlanId);
        Assert.Equal(personId, pp.PersonId);
    }

    [Fact]
    public void Can_Set_And_Get_Snapshot_Fields()
    {
        var pp = new PlanParticipant
        {
            FullName = "Петренко Петро Петрович",
            RankName = "Сержант",
            PositionName = "Стрілець",
            UnitName = "Взвод 1 / Рота 2",
            Author = "tester"
        };

        Assert.Equal("Петренко Петро Петрович", pp.FullName);
        Assert.Equal("Сержант", pp.RankName);
        Assert.Equal("Стрілець", pp.PositionName);
        Assert.Equal("Взвод 1 / Рота 2", pp.UnitName);
        Assert.Equal("tester", pp.Author);
    }

    [Fact]
    public void Can_Add_Action_To_Actions_Collection()
    {
        var pp = new PlanParticipant
        {
            FullName = "X",
            RankName = "Y",
            PositionName = "Z",
            UnitName = "U"
        };

        var action = new PlanParticipantAction
        {
            Id = Guid.NewGuid(),
            PlanParticipantId = Guid.NewGuid(),
            PlanId = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.UtcNow,
            Location = "Локація",
            GroupName = "Група",
            CrewName = "Екіпаж",
            Author = "tester"
        };

        pp.Actions.Add(action);

        Assert.Single(pp.Actions);
        Assert.Same(action, pp.Actions.First());
    }

    [Fact]
    public void Can_Set_And_Get_Navigation_Properties()
    {
        var plan = new Plan { Id = Guid.NewGuid(), PlanNumber = "PLN-001" };
        var person = new Person { Id = Guid.NewGuid(), LastName = "Іваненко", FirstName = "Іван" };

        var pp = new PlanParticipant
        {
            Plan = plan,
            Person = person
        };

        Assert.Same(plan, pp.Plan);
        Assert.Same(person, pp.Person);
    }
}
