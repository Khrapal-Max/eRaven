//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EditKindOrderViewModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.StatusKindViewModels;

public class EditKindOrderViewModel
{
    [Required]
    public int Id { get; set; }

    [Required, MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int CurrentOrder { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Порядок не може бути відʼємним.")]
    public int NewOrder { get; set; }
}
