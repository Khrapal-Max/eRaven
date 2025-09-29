//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models.Projections;

/// <summary>
/// Проекція призначення на посаду.
/// </summary>
public sealed record PersonAssignmentProjection(
    Guid AssignmentId,
    Guid PositionUnitId,
    DateTime OpenUtc,
    DateTime? CloseUtc,
    string? Note,
    string? Author);
