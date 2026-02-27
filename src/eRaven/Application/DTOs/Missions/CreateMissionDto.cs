//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateMissionDto
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Enums;
using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.Missions;

/// <summary>
/// DTO вводу для створення місії (Mission).
/// Використовується UI (drawer/page) для створення нового запису.
/// </summary>
public class CreateMissionDto
{
    [Required(ErrorMessage = "Позиційний район обов'язковий.")]
    [StringLength(140, MinimumLength = 2, ErrorMessage = "2–140 символів.")]
    public string PositionArea { get; set; } = string.Empty;

    [StringLength(140, ErrorMessage = "До 140 символів.")]
    public string? NamePoint { get; set; }

    /// <summary>
    /// Каталожна мета (рядок із каталогу TypeDronetCatalog).
    /// </summary>
    [StringLength(512)]
    public string? DroneName { get; set; }

    public MissionModeDto MissionMode { get; set; } = MissionModeDto.Day;

    /// <summary>
    /// Каталожна мета (рядок із каталогу TargetCatalog).
    /// </summary>
    [Required(ErrorMessage = "Мета обов'язкова.")]
    [StringLength(200, MinimumLength = 2)]
    public string Target { get; set; } = string.Empty;
}
