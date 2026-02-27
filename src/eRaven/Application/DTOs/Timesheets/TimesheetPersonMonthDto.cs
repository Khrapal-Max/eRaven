//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPersonMonthDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets;

/// <summary>
/// Персональний табель за місяць:
/// <list type="bullet">
/// <item><description><see cref="Days"/> — derived матриця по днях (календар).</description></item>
/// <item><description><see cref="Entries"/> — source of truth (інтервали подій).</description></item>
/// </list>
/// </summary>
public sealed record TimesheetPersonMonthDto(
    TimesheetPersonInfoDto Person,
    DateTime UpdatedAtUtc,
    IReadOnlyList<TimesheetDaySnapshotDto> Days,
    IReadOnlyList<TimesheetPersonEntryRowDto> Entries);
