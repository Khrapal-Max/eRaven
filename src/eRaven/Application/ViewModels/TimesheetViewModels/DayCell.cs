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

public sealed class DayCell
{
    public string? Code { get; set; }  // "30", "В", "нб", "РОЗПОР", "ВДР"…
    public string? Title { get; set; }  // людська назва (StatusKind.Name)
    public string? Note { get; set; }  // нотатка (тільки для тултіпів у вебі)
}
