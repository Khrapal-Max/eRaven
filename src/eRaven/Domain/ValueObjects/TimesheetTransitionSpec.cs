//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionSpec
//-----------------------------------------------------------------------------

namespace eRaven.Domain.ValueObjects;

/// <summary>
/// Опис дозволеного переходу: To + як трактувати дату.
/// StartShiftDays: 0 = "З дати події", 1 = "Ще поточний".
/// </summary>
public sealed record TimesheetTransitionSpec(Guid ToCodeId, int StartShiftDays);