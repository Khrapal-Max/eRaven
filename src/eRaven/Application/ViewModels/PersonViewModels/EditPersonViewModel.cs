//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// EditPersonViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Person;
using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.ViewModels.PersonViewModels;

public sealed class EditPersonViewModel
{

    [Required, MaxLength(128)]
    public string LastName { get; set; } = default!;

    [Required, MaxLength(128)]
    public string FirstName { get; set; } = default!;

    [MaxLength(128)]
    public string? MiddleName { get; set; }

    [Required]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "РНОКПП має містити рівно 10 символів.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "РНОКПП має складатися лише з цифр.")]
    public string Rnokpp { get; set; } = default!;

    [Required, MaxLength(64)]
    public string Rank { get; set; } = default!; // ОБОВ’ЯЗКОВО

    [Required, MaxLength(128)]
    public string BZVP { get; set; } = default!; // ОБОВ’ЯЗКОВО

    [MaxLength(128)]
    public string? Weapon { get; set; }

    [MaxLength(64)]
    public string? Callsign { get; set; }

    public static EditPersonViewModel From(Person p) => new()
    {
        LastName = p.LastName,
        FirstName = p.FirstName,
        MiddleName = p.MiddleName,
        Rnokpp = p.Rnokpp,
        Rank = p.Rank,
        BZVP = p.BZVP,
        Weapon = p.Weapon,
        Callsign = p.Callsign
    };
}
