//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateTimesheetEntryCommand
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Commands.Timesheet;

public sealed record CreateTimesheetEntryCommand(
    Guid PersonId,
    TimesheetLane Lane,
    string Code,
    DateOnly From,
    DateOnly? To,
    string? Reference,
    string? Note,
    string Author,
    DateTime NowUtc);
