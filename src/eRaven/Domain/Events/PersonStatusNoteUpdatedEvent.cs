//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public sealed record PersonStatusNoteUpdatedEvent(
    Guid PersonId,
    Guid StatusId,
    DateTime OccurredAtUtc,
    string? Note) : IPersonEvent;
