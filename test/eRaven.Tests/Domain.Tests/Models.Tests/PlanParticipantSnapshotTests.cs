//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanParticipantSnapshotTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models.Tests;

public class PlanParticipantSnapshotTests
{
    [Fact]
    public void Ctor_Assigns_All_Basic_Fields()
    {
        // arrange
        var planId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var when = new DateTime(2025, 01, 01, 10, 00, 00, DateTimeKind.Utc);

        // act
        var s = new PlanParticipantSnapshot
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            PersonId = personId,
            FullName = "Іванов Іван Іванович",
            Rank = "сержант",
            PositionSnapshot = "механік відділ В цех А",
            Weapon = "АК-74 №123",
            Callsign = "Сокіл",
            StatusKindId = 1,
            StatusKindCode = "READY",
            Author = "tester",
            RecordedUtc = when
        };

        // assert
        Assert.Equal(planId, s.PlanId);
        Assert.Equal(personId, s.PersonId);
        Assert.Equal("Іванов Іван Іванович", s.FullName);
        Assert.Equal("сержант", s.Rank);
        Assert.Equal("механік відділ В цех А", s.PositionSnapshot);
        Assert.Equal("АК-74 №123", s.Weapon);
        Assert.Equal("Сокіл", s.Callsign);
        Assert.Equal(1, s.StatusKindId);
        Assert.Equal("READY", s.StatusKindCode);
        Assert.Equal("tester", s.Author);
        Assert.Equal(when, s.RecordedUtc);
    }

    [Fact]
    public void Default_RecordedUtc_Is_Set_Close_To_Now()
    {
        // arrange
        var before = DateTime.UtcNow;

        // act
        var s = new PlanParticipantSnapshot
        {
            PlanId = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            FullName = "Петров Петро",
        };

        var after = DateTime.UtcNow;

        // assert (нестрогий інтервал, щоб тест не флакував)
        Assert.True(s.RecordedUtc >= before && s.RecordedUtc <= after,
            $"RecordedUtc {s.RecordedUtc:o} не в межах [{before:o} .. {after:o}]");
    }
}
