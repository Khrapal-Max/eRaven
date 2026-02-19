//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Timesheets;

public sealed record TransitionTimesheetStateCommand(
    Guid PersonId,
    DateOnly AnchorDate,
    DateOnly InputDate,
    Guid NextCode,
    string? Reference,
    string? Note,
    bool IsCorrection,
    string Author,
    DateTime NowUtc);
