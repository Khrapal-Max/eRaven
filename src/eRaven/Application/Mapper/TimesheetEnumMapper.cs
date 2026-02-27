//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEnumMapper
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using eRaven.Domain.Enums;

namespace eRaven.Application.Mapper;

internal static class TimesheetEnumMapper
{
    public static RoleCodeDto MapRole(RoleCode role)
        => role switch
        {
            RoleCode.TransitionCode => RoleCodeDto.TransitionCode,
            RoleCode.EmergencyCode => RoleCodeDto.EmergencyCode,
            RoleCode.SystemCode => RoleCodeDto.SystemCode,
            _ => RoleCodeDto.TransitionCode
        };

    public static RoleCode ToDomain(RoleCodeDto role)
        => role switch
        {
            RoleCodeDto.TransitionCode => RoleCode.TransitionCode,
            RoleCodeDto.EmergencyCode => RoleCode.EmergencyCode,
            RoleCodeDto.SystemCode => RoleCode.SystemCode,
            _ => RoleCode.TransitionCode
        };

    public static TimesheetUiStyleDto MapStyle(TimesheetUiStyle style)
        => style switch
        {
            TimesheetUiStyle.Warning => TimesheetUiStyleDto.Warning,
            TimesheetUiStyle.Ready => TimesheetUiStyleDto.Ready,
            TimesheetUiStyle.Danger => TimesheetUiStyleDto.Danger,
            TimesheetUiStyle.SystemFact => TimesheetUiStyleDto.SystemFact,
            TimesheetUiStyle.NotInTimesheet => TimesheetUiStyleDto.NotInTimesheet,
            _ => TimesheetUiStyleDto.Warning
        };

    public static TimesheetUiStyle ToDomain(TimesheetUiStyleDto style)
        => style switch
        {
            TimesheetUiStyleDto.Warning => TimesheetUiStyle.Warning,
            TimesheetUiStyleDto.Ready => TimesheetUiStyle.Ready,
            TimesheetUiStyleDto.Danger => TimesheetUiStyle.Danger,
            TimesheetUiStyleDto.SystemFact => TimesheetUiStyle.SystemFact,
            TimesheetUiStyleDto.NotInTimesheet => TimesheetUiStyle.NotInTimesheet,
            _ => TimesheetUiStyle.Warning
        };
}
