//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// Табель (місяць/рік) + експорт через ExcelExportButton без зайвих «хелперів»
// Логіка:
//   • показуємо тільки тих, у кого НЕ весь місяць «нб/РОЗПОР» і не весь місяць порожньо
//   • початкові «дірки» до першого коду за місяць → заповнюємо «нб»
//   • кольори для кодів, тултіп = "код — назва: нотатка"
//   • експорт: TimesheetExportRow з Day01..Day31 (щоб ExcelService зробив колонки)
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.TimesheetViewModels;

public sealed class TimesheetRow
{
    public Guid PersonId { get; set; }
    public string? FullName { get; set; }
    public string? Rank { get; set; }
    public string? Rnokpp { get; set; }
    public DayCell[] Days { get; set; } = [];
}
