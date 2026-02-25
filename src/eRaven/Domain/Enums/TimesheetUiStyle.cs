//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetUiStyle
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Enums;

/// <summary>
/// Семантичний стиль для UI/експорту (не бізнес-логіка переходів).
/// </summary>
public enum TimesheetUiStyle
{
    Warning = 0,
    Ready = 1,
    Danger = 2,
    SystemFact = 3,
    NotInTimesheet = 4
}