//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionCreateDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.DTOs.Timesheet;

/// <summary>
/// UI DTO for creating a timesheet entry (event) from month grid.
/// </summary>
public sealed record TimesheetTransitionCreateDto(
    Guid PersonId,
    TimesheetLane Lane,
    DateOnly AnchorDate,   // дата, з якої відкрили дравер (стан "на день")
    DateOnly InputDate,    // дата, яку вводить юзер (meaning залежить від поточного коду)
    string NextCode,
    string? Reference,
    string? Note
);
