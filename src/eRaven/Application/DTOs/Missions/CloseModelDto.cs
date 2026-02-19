//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseModelDto
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.Missions;

/// <summary>
/// DTO вводу для закриття місії (Mission).
/// Використовується UI (drawer/page) для закриття нового запису.
/// </summary>
public sealed class CloseModelDto
{
    [Required]
    public DateOnly ClosedAt { get; set; }
}
