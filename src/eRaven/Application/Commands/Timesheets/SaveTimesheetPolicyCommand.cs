//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SaveTimesheetPolicyCommand
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheets.Policy;

namespace eRaven.Application.Commands.Timesheets;

/// <summary>
/// Команда збереження політики коду (назва, порядок, дозволи тощо).
/// </summary>
public sealed record SaveTimesheetPolicyCommand(
    Guid CodeId,
    string Title,
    string? Description,
    int SortOrder,
    int Priority,
    bool IsTerminal,
    IReadOnlyCollection<TimesheetTransitionSpecDto> AllowedTransitions,
    string Author,
    DateTime NowUtc);