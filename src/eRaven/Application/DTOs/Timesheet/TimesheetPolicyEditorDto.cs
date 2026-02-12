//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyEditorDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetPolicyEditorDto(
    TimesheetCodeDto Code,
    IReadOnlyList<TimesheetTransitionSpecDto> AllowedTransitions
);
