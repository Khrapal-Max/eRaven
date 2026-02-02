//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetDayStateDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetDayStateDto(
  string Code,
  string? Reference,
  string? Note
);
