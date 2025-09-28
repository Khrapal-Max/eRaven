//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePersonViewModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.PersonViewModels;

public class CreatePersonViewModel
{
    [Required, MaxLength(128)]
    public string LastName { get; set; } = default!;

    [Required, MaxLength(128)]
    public string FirstName { get; set; } = default!;

    [MaxLength(128)]
    public string? MiddleName { get; set; }

    [Required, MaxLength(10)]
    public string Rnokpp { get; set; } = default!;

    [Required, MaxLength(64)]
    public string Rank { get; set; } = default!;

    [Required, MaxLength(64)]
    public string BZVP { get; set; } = default!;

    [MaxLength(64)]
    public string? Weapon { get; set; }

    [MaxLength(64)]
    public string? Callsign { get; set; }
}
