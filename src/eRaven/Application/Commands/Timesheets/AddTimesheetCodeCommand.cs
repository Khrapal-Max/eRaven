//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddTimesheetCodeCommand
//-----------------------------------------------------------------------------

namespace eRaven.Application.Commands.Timesheets;

using eRaven.Domain.Enums;

/// <summary>
/// Команда створення коду для епізода табеля.
/// </summary>
public sealed record AddTimesheetCodeCommand(
    string Code,
    string Title,
    string? Description,
    int SortOrder,
    int Priority,
    bool IsTerminal,
    RoleCode RoleCode,
    TimesheetUiStyle UiStyle,
    string Author,
    DateTime NowUtc);