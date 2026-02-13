//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetRangeDayDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

/// <summary>
/// Запис для показу дня в діапазоні місяця в табличці. 
/// Включає дату, код, посилання на код та інші дані для відображення.
/// </summary>
public sealed record TimesheetRangeDayDto(
    DateOnly Date,
    Guid CodeId,
    string Code,
    string? Reference);
