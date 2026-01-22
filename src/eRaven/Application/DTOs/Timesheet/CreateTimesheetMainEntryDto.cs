//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateTimesheetMainEntryDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

public sealed record CreateTimesheetMainEntryDto(
    Guid PersonId,
    DateOnly From,
    DateOnly? To,
    string Code,
    string? Reference,
    string? Note
);
