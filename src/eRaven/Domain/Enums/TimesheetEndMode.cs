//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEndMode
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Enums;

/// <summary>
/// UI/Policy hint: whether "To" is required for a code.
/// </summary>
public enum TimesheetEndMode
{
    /// <summary>To optional (open-ended allowed).</summary>
    PeriodOptional = 0,

    /// <summary>To required (must be a closed period).</summary>
    PeriodRequired = 1
}
