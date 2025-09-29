//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public sealed record PersonStatusClearedEvent(
    Guid PersonId,
    Guid StatusId,
    DateTime OccurredAtUtc,
    string? Author) : IPersonEvent;
