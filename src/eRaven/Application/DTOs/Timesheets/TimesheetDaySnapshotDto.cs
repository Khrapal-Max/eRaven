//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetDaySnapshotDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Timesheets;

/// <summary>
/// Єдиний “стан дня” для табеля (UI).
/// </summary>
public sealed record TimesheetDaySnapshotDto(
    DateOnly Date,
    Guid? CodeId,
    string Code,
    string? Reference,
    string? Note,
    bool IsDerived,
    bool IsChangePoint,
    TimesheetUiStyleDto UiStyle);
