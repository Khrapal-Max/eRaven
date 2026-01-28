//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDraftDto
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Nodel DTO для створення чернетки документу плану бойових завдань.
/// Викликається з PlanningCreateDraftDocumentDrawer.
/// </summary>
public class CreateCombatTaskDraftDto
{
    [Required,
     MinLength(2, ErrorMessage = "Назва повина бути більше 2 літералів"),
     MaxLength(250, ErrorMessage = "Назва повина бути не більше 250 літералі")]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateOnly RecordedAt { get; set; }
}
