// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// -----------------------------------------------------------------------------
// PlanTests
// -----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Tests.Domain.Tests.Models.Helpers; // OrderTestsHelpers.MakePlan

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanTests
{
    // ---------- IsQuarterAligned ----------

    [Theory]
    [InlineData(2025, 1, 1, 0, 0, 0)]
    [InlineData(2025, 1, 1, 7, 15, 0)]
    [InlineData(2025, 1, 1, 12, 30, 0)]
    [InlineData(2025, 1, 1, 23, 45, 0)]
    public void IsQuarterAligned_ReturnsTrue_ForAlignedTimes(
        int y, int m, int d, int hh, int mm, int ss)
    {
        var dt = new DateTime(y, m, d, hh, mm, ss, DateTimeKind.Utc);
        Assert.True(Plan.IsQuarterAligned(dt));
    }

    [Theory]
    [InlineData(2025, 1, 1, 0, 1, 0)]   // 01
    [InlineData(2025, 1, 1, 7, 16, 0)]  // 16
    [InlineData(2025, 1, 1, 12, 44, 0)] // 44
    [InlineData(2025, 1, 1, 23, 45, 1)] // секунда != 0
    [InlineData(2025, 1, 1, 23, 45, 0, 500)] // мс != 0
    public void IsQuarterAligned_ReturnsFalse_ForMisalignedTimes(
        int y, int m, int d, int hh, int mm, int ss, int ms = 0)
    {
        var dt = new DateTime(y, m, d, hh, mm, ss, ms, DateTimeKind.Utc);
        Assert.False(Plan.IsQuarterAligned(dt));
    }

    // ---------- EnsureQuarterAligned ----------

    [Fact]
    public void EnsureQuarterAligned_DoesNotThrow_ForAlignedTime()
    {
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = "PL-100",
            Type = PlanType.Dispatch,
            PlannedAtUtc = new DateTime(2025, 1, 1, 17, 30, 0, DateTimeKind.Utc),
            TimeKind = PlanTimeKind.Start
        };

        var ex = Record.Exception(() => plan.EnsureQuarterAligned());
        Assert.Null(ex);
    }

    [Theory]
    [InlineData(2025, 1, 1, 17, 31, 0)]
    [InlineData(2025, 1, 1, 17, 30, 1)]
    public void EnsureQuarterAligned_Throws_ForNonAlignedTime(
        int y, int m, int d, int hh, int mm, int ss)
    {
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = "PL-101",
            Type = PlanType.Dispatch,
            PlannedAtUtc = new DateTime(y, m, d, hh, mm, ss, DateTimeKind.Utc),
            TimeKind = PlanTimeKind.Start
        };

        var ex = Assert.Throws<InvalidOperationException>(() => plan.EnsureQuarterAligned());
        Assert.Contains("00/15/30/45", ex.Message);
    }

    // ---------- Defaults & simple props ----------

    [Fact]
    public void NewPlan_DefaultState_IsOpen_AndParticipantsEmpty()
    {
        var plan = OrderTestsHelpers.MakePlan();

        Assert.Equal(PlanState.Open, plan.State);
        Assert.NotNull(plan.Participants);
        Assert.Empty(plan.Participants);
    }

    [Fact]
    public void Plan_Persists_TimeKind_Start_And_End()
    {
        var startPlan = OrderTestsHelpers.MakePlan(timeKind: PlanTimeKind.Start);
        var endPlan = OrderTestsHelpers.MakePlan(timeKind: PlanTimeKind.End);

        Assert.Equal(PlanTimeKind.Start, startPlan.TimeKind);
        Assert.Equal(PlanTimeKind.End, endPlan.TimeKind);
    }

    [Fact]
    public void Plan_CanStore_Basic_Metadata()
    {
        var plan = OrderTestsHelpers.MakePlan(
            planNumber: "PL-777",
            type: PlanType.Return,
            plannedAtUtc: new DateTime(2025, 2, 10, 9, 45, 0, DateTimeKind.Utc),
            timeKind: PlanTimeKind.End,
            location: "Місто Y",
            groupName: "Група B",
            toolType: "Інструмент Z"
        );

        Assert.Equal("PL-777", plan.PlanNumber);
        Assert.Equal(PlanType.Return, plan.Type);
        Assert.Equal(new DateTime(2025, 2, 10, 9, 45, 0, DateTimeKind.Utc), plan.PlannedAtUtc);
        Assert.Equal(PlanTimeKind.End, plan.TimeKind);
        Assert.Equal("Місто Y", plan.Location);
        Assert.Equal("Група B", plan.GroupName);
        Assert.Equal("Інструмент Z", plan.ToolType);
    }

    // ---------- PlanParticipantSnapshot у Plan ----------

    [Fact]
    public void Add_Single_Participant_Snapshot_To_Plan()
    {
        // arrange
        var plan = OrderTestsHelpers.MakePlan();
        var participant = new PlanParticipantSnapshot
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = Guid.NewGuid(),
            FullName = "Іванов Іван",
            Rank = "солдат",
            PositionSnapshot = "стрілець відділення 1",
            StatusKindId = 1,
            StatusKindCode = "READY",
            Author = "tester"
        };

        // act
        plan.Participants.Add(participant);

        // assert
        Assert.Single(plan.Participants);
        var saved = plan.Participants.Single();
        Assert.Equal(plan.Id, saved.PlanId);
        Assert.Equal("Іванов Іван", saved.FullName);
        Assert.Equal("солдат", saved.Rank);
        Assert.Equal("стрілець відділення 1", saved.PositionSnapshot);
        Assert.Equal(1, saved.StatusKindId);
        Assert.Equal("READY", saved.StatusKindCode);
    }

    [Fact]
    public void Add_Multiple_Participants_Snapshots_To_Plan()
    {
        // arrange
        var plan = OrderTestsHelpers.MakePlan();

        var p1Id = Guid.NewGuid();
        var p2Id = Guid.NewGuid();

        var s1 = new PlanParticipantSnapshot
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = p1Id,
            FullName = "Перший Боєць",
            Rank = "сержант",
            StatusKindId = 1,
            StatusKindCode = "READY"
        };

        var s2 = new PlanParticipantSnapshot
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = p2Id,
            FullName = "Другий Боєць",
            Rank = "рядовий",
            StatusKindId = 1,
            StatusKindCode = "READY"
        };

        // act
        plan.Participants.Add(s1);
        plan.Participants.Add(s2);

        // assert
        Assert.Equal(2, plan.Participants.Count);
        Assert.Contains(plan.Participants, x => x.PersonId == p1Id && x.FullName == "Перший Боєць");
        Assert.Contains(plan.Participants, x => x.PersonId == p2Id && x.FullName == "Другий Боєць");
        Assert.All(plan.Participants, x => Assert.Equal(plan.Id, x.PlanId));
    }

    [Fact]
    public void Participants_List_Is_Independent_Per_Item()
    {
        // arrange
        var plan = OrderTestsHelpers.MakePlan();

        var s = new PlanParticipantSnapshot
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = Guid.NewGuid(),
            FullName = "Змінний Боєць",
            Rank = "ефрейтор",
            StatusKindId = 1,
            StatusKindCode = "READY"
        };

        plan.Participants.Add(s);

        // act: змінюємо локальну змінну, посилання в списку має відображати те саме (це очікувана поведінка)
        s.FullName = "Оновлене Ім’я";

        // assert
        Assert.Equal("Оновлене Ім’я", plan.Participants.Single().FullName);
    }
}
