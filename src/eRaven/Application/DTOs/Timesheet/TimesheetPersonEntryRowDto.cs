//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonEntryRowDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetPersonEntryRowDto(
TimesheetLane Lane,
string Code,
DateOnly From,
DateOnly? To,
string? Reference,
string? Note);
