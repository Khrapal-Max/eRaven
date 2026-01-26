//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateDraftModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Nodel DTO для створення чернетки плану бойового завдання.
/// Викликається з PlanningCreateDraftDocumentDrawer.
/// </summary>
public sealed class CreateDraftModelDto
{
    [Required]
    public DateOnly RecordedAt { get; set; }

    [Required]
    public DateOnly PlanningDate { get; set; }

    [Required, MinLength(2), MaxLength(250)]
    public string PlanningDocTitle { get; set; } = string.Empty;
}
