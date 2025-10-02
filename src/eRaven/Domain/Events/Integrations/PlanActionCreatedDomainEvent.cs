//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionCreatedDomainEvent
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Events.Integrations;

public record PlanActionCreatedDomainEvent(
    Guid PersonId,
    Guid PlanActionId,
    DateTime EffectiveAt,
    MoveType MoveType) : IDomainEvent
{
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
}
