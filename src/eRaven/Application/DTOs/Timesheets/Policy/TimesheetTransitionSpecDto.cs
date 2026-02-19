//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionSpecDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheets.Policy;

/// <summary>
/// Опис дозволеного переходу: To + як трактувати дату.
/// StartShiftDays: 0 = "З дати події", 1 = "Ще поточний".
/// </summary>
public sealed record TimesheetTransitionSpecDto(Guid ToCodeId, int StartShiftDays);