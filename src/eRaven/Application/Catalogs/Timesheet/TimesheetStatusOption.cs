//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetStatusOption
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.Catalogs.Timesheet;

/// <summary>
/// Довідниковий статус табелю для UI.
/// Code:
/// - fixed: реальний код, який пишемо в табель (наприклад "ВДР").
/// - template: підказка для вводу оператором (наприклад "Номер рапорта").
/// </summary>
public sealed record TimesheetStatusOption(
    string Title,
    TimesheetLane Lane,
    string Code,
    bool IsCodeTemplate = false
);
