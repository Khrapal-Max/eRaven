//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetTransitionOptionDto
//-----------------------------------------------------------------------------

namespace eRaven.Application.DTOs.Timesheet;

/// <summary>
/// Опція для селекта "дозволені переходи" (з уже підставленим правилом 0/1).
/// </summary>
public sealed record TimesheetTransitionOptionDto(
    string Code,
    string Title,
    int StartShiftDays)
{
    public string Display => $"{Code} — {Title}";
}
