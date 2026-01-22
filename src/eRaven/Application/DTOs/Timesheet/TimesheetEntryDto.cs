//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

public sealed record TimesheetEntryDto(
    Guid Id,
    TimesheetLane Lane,
    string Code,
    DateOnly From,
    DateOnly? To,
    string? Reference,
    string? Note
);
