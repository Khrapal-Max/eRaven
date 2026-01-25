//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDayGroupDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

public sealed record PlanningDayGroupDto(
    string PositionalArea,
    string GroupName,
    string? AssetType,
    string Mode,
    string Goal,
    IReadOnlyList<PlanningDayPersonDto> Persons
);