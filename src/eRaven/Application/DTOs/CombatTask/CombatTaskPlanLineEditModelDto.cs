//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanLineEditModelDto
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.CombatTask;

/// <summary>
/// Модель для UI-форми створення/редагування рядка плану (Draft).
/// Окрема від record InputDto, щоб EditForm мав settable-поля + DataAnnotations.
/// </summary>
public sealed class CombatTaskPlanLineEditModelDto
{
    [Required] 
    public CombatTaskPlanLineKind Kind { get; set; } = CombatTaskPlanLineKind.Start;
    [Required] 
    public Guid PersonId { get; set; }
    [Required] 
    public DateOnly ActionDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required, StringLength(20)] 
    public string RNOKPP { get; set; } = string.Empty;
    [Required, StringLength(200, MinimumLength = 2)] 
    public string FullName { get; set; } = string.Empty;
    [StringLength(80)] 
    public string? Rank { get; set; }
    [StringLength(200)]
    public string? Position { get; set; }
    [StringLength(80)] 
    public string? Weapon { get; set; }
    [StringLength(80)]
    public string? Callsign { get; set; }

    [Required, StringLength(140)] 
    public string PositionalArea { get; set; } = string.Empty;
    [Required, StringLength(140)]
    public string GroupName { get; set; } = string.Empty;
    [StringLength(120)] 
    public string? AssetType { get; set; }

    public CombatTaskMode Mode { get; set; } = CombatTaskMode.Day;

    [Required, StringLength(160)] 
    public string Goal { get; set; } = string.Empty;

    public bool IsActual { get; set; } = true;

    [StringLength(500)] 
    public string? Note { get; set; }

    /// <summary>Тільки для End (може бути null → Guid.Empty у DB і auto resolve при Post).</summary>
    public Guid? AssignmentId { get; set; }
}