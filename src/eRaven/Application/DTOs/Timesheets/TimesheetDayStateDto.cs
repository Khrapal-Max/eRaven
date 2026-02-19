//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetDayStateDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets;

/// <summary>
/// Запис стану дня в табелі
/// </summary>
public sealed record TimesheetDayStateDto(
  Guid CodeId,
  string Code,
  string? Reference,
  string? Note
);
