//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskCommand
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;

namespace eRaven.Application.Commands.CombatTask;

public sealed record CreateCombatTaskCommand(
     Guid DocumentId,
     Guid MissionId,
     string SourceDocument,
     ICollection<CombatTaskDetailsDto> CombatTaskDetails);
