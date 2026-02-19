//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEditorDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

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
    DocumentStatus Status,
    DateOnly RecordedAt,
    IReadOnlyList<CombatTaskMissionBlockDto> Missions);
