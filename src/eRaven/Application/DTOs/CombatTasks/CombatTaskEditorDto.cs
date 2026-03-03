//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEditorDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.CombatTasks;

/// <summary>
/// DTO для редактора документа бойових завдань:
/// - групування по місії (CombatTask),
/// - всередині рядки Start/End по особам.
/// </summary>
public sealed record CombatTaskEditorDto(
    Guid DocumentId,
    string DocumentName,
    string? Description,
    DocumentStatusDto Status,
    DateOnly RecordedAt,
    IReadOnlyList<CombatTaskMissionBlockDto> Missions);
