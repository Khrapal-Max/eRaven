//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDraftDto
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.CombatTasks.Models;

/// <summary>
/// Nodel DTO для створення чернетки документу плану бойових завдань.
/// Викликається з PlanningCreateDraftDocumentDrawer.
/// </summary>
public class CreateCombatTaskDocumentModel
{
    [Required,
     MinLength(2, ErrorMessage = "Назва повина бути більше 2 літералів"),
     MaxLength(512, ErrorMessage = "Назва повина бути не більше 512 літералі")]
    public string OrderTitle { get; set; } = string.Empty;

    [MaxLength(512, ErrorMessage = "Назва повина бути не більше 512 літералі")]
    public string Description { get; set; } = string.Empty;

    [Required]
    public DateOnly RecordedAt { get; set; }
}
