//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanParticipantSnapshotTests (final; з RNOKPP як обов'язковим з точки зору моделі)
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class PlanParticipantSnapshotTests
{
    [Fact(DisplayName = "PPS: дефолти — GUID-и порожні, рядки null, RecordedUtc ~ now (UTC)")]
    public void Defaults_AreCorrect()
    {
        var s = new PlanParticipantSnapshot();

        Assert.Equal(Guid.Empty, s.Id);
        Assert.Equal(Guid.Empty, s.PlanElementId);
        Assert.Equal(Guid.Empty, s.PersonId);

        Assert.Null(s.FullName);
        Assert.Null(s.Rnokpp);
        Assert.Null(s.Rank);
        Assert.Null(s.PositionSnapshot);
        Assert.Null(s.Weapon);
        Assert.Null(s.Callsign);
        Assert.Null(s.StatusKindId);
        Assert.Null(s.StatusKindCode);
        Assert.Null(s.Author);

        Assert.Equal(DateTimeKind.Utc, s.RecordedUtc.Kind);
        Assert.InRange(DateTime.UtcNow - s.RecordedUtc, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "PPS: можна задати та прочитати всі основні поля")]
    public void Can_Set_And_Read_Properties()
    {
        var id = Guid.NewGuid();
        var elementId = Guid.NewGuid();
        var personId = Guid.NewGuid();
        var when = new DateTime(2025, 9, 10, 18, 5, 0, DateTimeKind.Utc);

        var s = new PlanParticipantSnapshot
        {
            Id = id,
            PlanElementId = elementId,
            PersonId = personId,
            FullName = "Іванов Іван Іванович",
            Rnokpp = "1111111111",
            Rank = "сержант",
            PositionSnapshot = "Командир відділення",
            Weapon = "АК-74",
            Callsign = "Іванов",
            StatusKindId = 30,
            StatusKindCode = "30",
            Author = "tester",
            RecordedUtc = when
        };

        Assert.Equal(id, s.Id);
        Assert.Equal(elementId, s.PlanElementId);
        Assert.Equal(personId, s.PersonId);
        Assert.Equal("Іванов Іван Іванович", s.FullName);
        Assert.Equal("1111111111", s.Rnokpp);
        Assert.Equal("сержант", s.Rank);
        Assert.Equal("Командир відділення", s.PositionSnapshot);
        Assert.Equal("АК-74", s.Weapon);
        Assert.Equal("Іванов", s.Callsign);
        Assert.Equal(30, s.StatusKindId);
        Assert.Equal("30", s.StatusKindCode);
        Assert.Equal("tester", s.Author);
        Assert.Equal(when, s.RecordedUtc);
    }
}
