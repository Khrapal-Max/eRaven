//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEditorDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// DTO для редактора документа бойових завдань:
/// - групування по місії (CombatTask),
/// - всередині рядки Start/End по особам.
/// </summary>
public sealed record CombatTaskEditorDto(
    Guid DocumentId,
    string DocumentName,
    DocumentStatus Status,
    DateOnly RecordedAt,
    IReadOnlyList<CombatTaskMissionBlockDto> Missions);
