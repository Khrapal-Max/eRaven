//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddTimesheetCodeModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.Timesheet.Models;

/// <summary>
/// Модель форми для створення коду табеля.
/// </summary>
public sealed class AddTimesheetCodeModel
{
    [Required(ErrorMessage = "Код обов'язковий.")]
    [StringLength(20, ErrorMessage = "Код задовгий (макс 20 символів).")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Назва обов'язкова.")]
    [StringLength(120, ErrorMessage = "Назва задовга (макс 120 символів).")]
    public string Title { get; set; } = string.Empty;

    [StringLength(400, ErrorMessage = "Опис задовгий (макс 400 символів).")]
    public string? Description { get; set; }

    [Range(0, 10_000, ErrorMessage = "Порядок має бути >= 0.")]
    public int SortOrder { get; set; }

    [Range(0, 10_000, ErrorMessage = "Пріоритет має бути >= 0.")]
    public int Priority { get; set; }

    public bool IsTerminal { get; set; }
}
