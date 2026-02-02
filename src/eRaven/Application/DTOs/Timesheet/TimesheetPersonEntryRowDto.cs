//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonEntryRowDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetPersonEntryRowDto(
string Code,
DateOnly From,
DateOnly? To,
string? Reference,
string? Note);
