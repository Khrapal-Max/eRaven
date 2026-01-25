//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningMonthAssignmentRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.CombatTask;

public sealed record PlanningMonthAssignmentRowDto(
Guid AssignmentId,
Guid PersonId,
string FullName,
string RNOKPP,
string? Rank,
string? Position,
string? Weapon,
string? Callsign,

DateOnly PlanningDate,
string PlanningDocTitle,

DateOnly StartDate,
DateOnly? EndDate,

string PositionalArea,
string GroupName,
string? AssetType,
string Mode,
string Goal,

bool IsActual,
CombatTaskPlanDocumentStatus DocumentStatus);
