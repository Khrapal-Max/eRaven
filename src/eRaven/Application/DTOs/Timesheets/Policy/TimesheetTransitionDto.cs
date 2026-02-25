//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets.Policy;

public sealed record TimesheetTransitionDto(
    Guid ToCodeId,
    int StartShiftDays);