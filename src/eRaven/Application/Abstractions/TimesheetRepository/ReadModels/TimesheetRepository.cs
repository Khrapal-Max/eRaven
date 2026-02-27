//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetDayRm
//-----------------------------------------------------------------------------

namespace eRaven.Application.Abstractions.TimesheetRepository.ReadModels;

using eRaven.Domain.Enums;

/// <summary>
/// Стан дня у табелі (timesheet-only).
/// </summary>
public sealed record TimesheetDayRm(
    Guid TimesheetId,
    DateOnly DateOfDay,
    Guid? CodeId,
    string Code,
    string? Reference,
    string? Note,
    bool IsDerived,
    bool IsChangePoint,
    TimesheetUiStyle UiStyle);
