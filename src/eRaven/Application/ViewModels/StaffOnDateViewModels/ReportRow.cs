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

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.StaffOnDateViewModels;

public sealed class ReportRow
{
    // Посада
    [Display(Name = "Індекс")]
    public string? PositionCode { get; set; }

    [Display(Name = "Посада (коротка)")]
    public string? PositionShort { get; set; }

    [Display(Name = "Повна назва")]
    public string? PositionFull { get; set; }

    [Display(Name = "ВОС")]
    public string? SpecialNumber { get; set; }

    // Людина
    [Display(Name = "ПІБ")]
    public string? FullName { get; set; }

    [Display(Name = "Звання")]
    public string? Rank { get; set; }

    [Display(Name = "РНОКПП")]
    public string? Rnokpp { get; set; }

    [Display(Name = "Позивний")]
    public string? Callsign { get; set; }

    [Display(Name = "БЗВП")]
    public string? BZVP { get; set; }

    [Display(Name = "Зброя")]
    public string? Weapon { get; set; }

    // Статус на дату
    [Display(Name = "Код статусу")]
    public string? StatusCode { get; set; }

    [Display(Name = "Статус")]
    public string? StatusName { get; set; }

    [Display(Name = "Нотатка")]
    public string? StatusNote { get; set; }
}
