//----------------------------------------------------------------------------- 
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//----------------------------------------------------------------------------- 
//----------------------------------------------------------------------------- 
// PersonStatusHistoryItem
//----------------------------------------------------------------------------- 

namespace eRaven.Application.ViewModels.PersonStatusViewModels;

/// <summary>
/// Read-only snapshot for a status change in the history timeline.
/// </summary>
/// <param name="StatusId">Unique identifier of the status entry.</param>
/// <param name="StatusKindId">Identifier of the dictionary status kind.</param>
/// <param name="StatusCode">Dictionary code of the status.</param>
/// <param name="StatusName">Human readable name of the status.</param>
/// <param name="OpenDateUtc">Moment when the status was opened (UTC).</param>
/// <param name="IsActive">Whether the status is currently marked as active.</param>
/// <param name="Sequence">Sequence number for the same open date.</param>
/// <param name="Note">Optional user note.</param>
/// <param name="Author">Optional author of the change.</param>
/// <param name="SourceDocumentId">Identifier of the source document (if any).</param>
/// <param name="SourceDocumentType">Type of the source document (if any).</param>
public sealed record PersonStatusHistoryItem(
    Guid StatusId,
    int StatusKindId,
    string? StatusCode,
    string? StatusName,
    DateTime OpenDateUtc,
    bool IsActive,
    short Sequence,
    string? Note,
    string? Author,
    Guid? SourceDocumentId,
    string? SourceDocumentType);
