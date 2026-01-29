//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDocumentDetailsDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO змісту документа ланування.
///
/// Призначення:
/// - швидкий список місій в документі.
/// </summary>
public sealed record CombatTaskDocumentDetailsDto(
    Guid DocumentId,
    string OrderTitle,
    DocumentStatus Status,
    DateOnly RecordedAt,
    string CanceledReason,
    IReadOnlyList<MissionActionDetailsDto> Actions);