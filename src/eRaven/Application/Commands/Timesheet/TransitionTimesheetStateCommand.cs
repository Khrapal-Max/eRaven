//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommand
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Commands.Timesheet;

public sealed record TransitionTimesheetStateCommand(
    Guid PersonId,
    TimesheetLane Lane,
    DateOnly AnchorDate,
    DateOnly InputDate,
    string NextCode,
    string? Reference,
    string? Note,
    string Author,
    DateTime NowUtc);
