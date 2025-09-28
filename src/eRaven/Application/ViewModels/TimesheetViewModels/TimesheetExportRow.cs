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

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.TimesheetViewModels;

// ---------------- Експорт: модель ----------------
public sealed class TimesheetExportRow
{
    [Display(Name = "ПІБ")]
    public string? FullName { get; set; }

    [Display(Name = "Звання")]
    public string? Rank { get; set; }

    [Display(Name = "РНОКПП")]
    public string? Rnokpp { get; set; }

    // Головне поле: динамічні дні місяця
    // ExcelService автоматично розгорне це в колонки 01..NN
    public string[] Days { get; set; } = [];
}
