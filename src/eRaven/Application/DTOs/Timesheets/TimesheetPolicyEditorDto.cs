//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetPolicyEditorDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets;

/// <summary>
/// Запис для показу на формі редагування політики табеля.
/// Містить код політики та перелік дозволених переходів між статусами табеля.
/// </summary>
public sealed record TimesheetPolicyEditorDto(
    TimesheetCodeDto Code,
    IReadOnlyList<TimesheetTransitionSpecDto> AllowedTransitions
);
