//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Person (Aggregate Root)
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models.Projections;

/// <summary>
/// Проекція статусу.
/// </summary>
public sealed record PersonStatusProjection(
    Guid StatusId,
    int StatusKindId,
    DateTime OpenDateUtc,
    bool IsActive,
    short Sequence,
    string? Note,
    string? Author,
    Guid? SourceDocumentId,
    string? SourceDocumentType);
