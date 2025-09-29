//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public sealed record PersonPositionAssignmentTouchedEvent(
    Guid PersonId,
    Guid AssignmentId,
    DateTime OccurredAtUtc,
    string? Note,
    string? Author) : IPersonEvent;
