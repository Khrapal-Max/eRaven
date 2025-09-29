//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models.Projections;

/// <summary>
/// Проекція картки людини.
/// </summary>
public sealed record PersonCardProjection(
    Guid PersonId,
    string Rnokpp,
    string Rank,
    string LastName,
    string FirstName,
    string? MiddleName,
    string FullName,
    Guid? PositionUnitId,
    int? StatusKindId,
    bool IsAttached,
    string? AttachedFromUnit,
    DateTime CreatedUtc,
    DateTime ModifiedUtc);
