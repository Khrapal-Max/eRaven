//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCandidateDto
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs;

public class CreateCandidateDto
{
    [Required(ErrorMessage = "РНОКПП обов'язковий")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "РНОКПП має містити рівно 10 цифр")]
    public string Rnokpp { get; set; } = string.Empty;

    [Required(ErrorMessage = "Прізвище обов'язкове")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ім’я обов'язкове")]
    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }
    public string? PlannedPosition { get; set; }
}
