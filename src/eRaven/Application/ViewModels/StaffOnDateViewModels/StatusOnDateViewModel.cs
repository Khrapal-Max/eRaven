// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// Reports → StaffOnDatePage (code-behind)
// Логіка:
//  • обираємо дату → “Побудувати” → збираємо усіх та їхній статус на дату
//  • виключаємо з таблиці коди "нб" і "РОЗПОР"
//  • сортування: спочатку за індексом посади (PositionUnit.Code), потім за повною назвою
//  • експорт: плоска модель без стилів/кольорів (ті самі колонки)
// -----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.StaffOnDateViewModels;

// ===================== View-models для сторінки =====================
public class StatusOnDateViewModel
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Note { get; set; }
}