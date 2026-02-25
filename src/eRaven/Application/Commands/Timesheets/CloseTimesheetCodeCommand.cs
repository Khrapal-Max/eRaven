//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseTimesheetCodeCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Timesheets;

/// <summary>
/// Команда закриття коду епізода табеля в довіднику.
/// </summary>
public sealed record CloseTimesheetCodeCommand(
    Guid CodeId,
    string Author,
    DateTime NowUtc);