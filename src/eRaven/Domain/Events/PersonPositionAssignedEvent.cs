//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Events;

public sealed record PersonPositionAssignedEvent(
    Guid PersonId,
    Guid AssignmentId,
    Guid PositionUnitId,
    DateTime OccurredAtUtc,
    string? Note,
    string? Author) : IPersonEvent;
