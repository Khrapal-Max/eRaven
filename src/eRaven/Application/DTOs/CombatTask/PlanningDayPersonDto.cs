//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDayPersonDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.CombatTask;

public sealed record PlanningDayPersonDto(
    Guid PersonId,
    string FullName,
    string RNOKPP,
    string? Rank,
    string? Position,
    string? Callsign,
    DateOnly StartDate,
    DateOnly? EndDate,
    string DocumentTitle);