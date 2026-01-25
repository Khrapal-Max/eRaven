//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetDayLaneStateDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetDayLaneStateDto(
  string Code,
  string? Reference,
  string? Note
);
