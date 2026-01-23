//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEndMode
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Enums;

public enum TimesheetEndMode
{
    /// <summary>
    /// може бути open-ended або [From..To]
    /// </summary>
    PeriodOptional = 0,
    /// <summary>
    /// To обов'язковий
    /// </summary>
    PeriodRequired = 1,
    /// <summary>
    /// одно-добовий запис
    /// </summary>
    SingleDay = 2     
}