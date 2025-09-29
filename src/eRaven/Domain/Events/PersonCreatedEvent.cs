//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public sealed record PersonCreatedEvent(
    Guid PersonId,
    string Rnokpp,
    string Rank,
    string LastName,
    string FirstName,
    string? MiddleName,
    string BZVP,
    string? Weapon,
    string? Callsign,
    bool IsAttached,
    string? AttachedFromUnit,
    DateTime OccurredAtUtc) : IPersonEvent;
