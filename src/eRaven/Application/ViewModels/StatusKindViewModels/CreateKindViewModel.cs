//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// NewKindViewModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.StatusKindViewModels;

/// <summary>
/// Модель для створення нового статусу
/// </summary>
/// <param name="Name"></param>
/// <param name="Code"></param>
/// <param name="Order"></param>
/// <param name="IsActive"></param>
public sealed class CreateKindViewModel
{
    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(16)]
    public string Code { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}