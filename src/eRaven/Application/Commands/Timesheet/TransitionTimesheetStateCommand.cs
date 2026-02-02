//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Timesheet;

public sealed record TransitionTimesheetStateCommand(
    Guid PersonId,
    DateOnly AnchorDate,
    DateOnly InputDate,
    string NextCode,
    string? Reference,
    string? Note,
    string Author,
    DateTime NowUtc);
