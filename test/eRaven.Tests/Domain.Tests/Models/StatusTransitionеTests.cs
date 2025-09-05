//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusTransitionTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Tests.Domain.Tests.Models;

public class StatusTransitionTests
{
    [Fact]
    public void Defaults_AreClrDefaults()
    {
        // Arrange
        var s = new StatusTransition();

        // Act & Assert (POCO рівень)
        Assert.Equal(0, s.Id);
        Assert.Equal(0, s.FromStatusKindId);
        Assert.Equal(0, s.ToStatusKindId);
        Assert.Null(s.FromStatusKind); // навігації не ініціалізовані доки не присвоїмо
        Assert.Null(s.ToStatusKind);
    }

    [Fact]
    public void CanSet_AndRead_AllProperties_WithNavigations()
    {
        // Arrange
        var from = new StatusKind { Id = 1, Name = "В районі", Code = "ВР" };
        var to = new StatusKind { Id = 2, Name = "В БР", Code = "ВБР" };

        var s = new StatusTransition
        {
            Id = 10,
            FromStatusKindId = from.Id,
            ToStatusKindId = to.Id,
            FromStatusKind = from,
            ToStatusKind = to
        };

        // Act & Assert
        Assert.Equal(10, s.Id);
        Assert.Equal(1, s.FromStatusKindId);
        Assert.Equal(2, s.ToStatusKindId);
        Assert.Same(from, s.FromStatusKind);
        Assert.Same(to, s.ToStatusKind);
        Assert.Equal(s.FromStatusKindId, s.FromStatusKind.Id);
        Assert.Equal(s.ToStatusKindId, s.ToStatusKind.Id);
    }

    [Fact]
    public void Poco_Allows_FromEqualsTo_ButDbConstraintShouldReject()
    {
        // Arrange (POCO дозволяє; БД має заборонити через ck_status_transitions_from_ne_to)
        var s = new StatusTransition
        {
            FromStatusKindId = 5,
            ToStatusKindId = 5
        };

        // Act & Assert (на рівні моделі це можливо)
        Assert.Equal(5, s.FromStatusKindId);
        Assert.Equal(5, s.ToStatusKindId);
    }
}
