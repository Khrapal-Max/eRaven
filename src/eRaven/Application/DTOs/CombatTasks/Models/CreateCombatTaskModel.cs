//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.CombatTasks.Models;

/// <summary>
/// Модель створення "групи участей" (CombatTask) у межах документа:
/// - SourceDocument: номер джерела (рапорт/наказ/документ),
/// - MissionId: обрана місія,
/// - Details: набір рядків Start/End по особам (light snapshot).
/// </summary>
public sealed class CreateCombatTaskModel
{
    [Required, MaxLength(30, ErrorMessage = "Назва (номер джерела) не більше 30 символів.")]
    public string SourceDocument { get; set; } = string.Empty;

    /// <summary>Обрана місія для групи.</summary>
    [Required(ErrorMessage = "Оберіть місію.")]
    public Guid MissionId { get; set; } = Guid.Empty;

    /// <summary>Рядки Start/End для осіб.</summary>
    public ICollection<CreateCombatTaskDetailsModel> Details { get; set; } = [];
}
