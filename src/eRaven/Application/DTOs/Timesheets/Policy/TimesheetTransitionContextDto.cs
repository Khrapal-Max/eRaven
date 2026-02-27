//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// TimesheetTransitionContextDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;

namespace eRaven.Application.DTOs.Timesheets.Policy;

/// <summary>
/// Контекст для UI переходу табеля на конкретну дату.
/// </summary>
public sealed record TimesheetTransitionContextDto(
    Guid PersonId,
    DateOnly OnDate,
    Guid? CurrentCodeId,
    string? CurrentCode,
    bool IsDerived,
    RoleCodeDto? CurrentRole,
    IReadOnlyList<TimesheetTransitionOptionDto> TransitionOptions,
    IReadOnlyList<TimesheetTransitionOptionDto> EmergencyOptions);
