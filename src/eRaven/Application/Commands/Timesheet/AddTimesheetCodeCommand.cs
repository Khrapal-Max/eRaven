//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddTimesheetCodeCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Timesheet;

public sealed record AddTimesheetCodeCommand(
    string Code,
    string Title,
    string? Description,
    int SortOrder,
    int Priority,
    bool IsTerminal,
    string Author,
    DateTime NowUtc);