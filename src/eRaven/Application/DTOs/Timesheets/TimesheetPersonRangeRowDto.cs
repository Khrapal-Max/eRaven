//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonRangeRowDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets;

/// <summary>
/// Єдиний рядок табеля для довільного періоду (день/тиждень/місяць).
/// </summary>
public sealed record TimesheetPersonRangeRowDto(
    TimesheetPersonInfoDto Person,
    IReadOnlyList<TimesheetDaySnapshotDto> Days);
