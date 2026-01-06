//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonWeaponChanged
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

/// <summary>
/// Зміна зброї у персони.
/// </summary>
public sealed record PersonWeaponChanged(
    Guid EventId,
    Guid AggregateId,
    DateOnly EffectiveDate,
    string? Weapon,
    string Author,
    DateTime OccurredAtUtc) : IDomainEvent;
