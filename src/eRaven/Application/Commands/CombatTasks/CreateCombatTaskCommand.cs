//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskCommand
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTasks;

namespace eRaven.Application.Commands.CombatTasks;

/// <summary>
/// Додає факти (snapshot-рядки Start/End) у блок місії документа бойових завдань
/// та запускає синхронізацію факту завдання з табелем.
/// </summary>
public sealed record CreateCombatTaskCommand(
     Guid DocumentId,
     Guid MissionId,
     string SourceDocument,
     ICollection<CombatTaskDetailsDto> CombatTaskDetails,
     string Author,
     DateTime NowUtc);
