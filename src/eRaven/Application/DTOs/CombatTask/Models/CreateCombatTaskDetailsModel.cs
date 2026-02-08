//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDetailsModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.CombatTask.Models;

/// <summary>
/// Рядок для створення CombatTaskDetails.
/// Важливо: PersonId потрібен для зв’язку з доменною особою.
/// </summary>
public sealed class CreateCombatTaskDetailsModel
{
    [Required(ErrorMessage = "PersonId обов'язковий.")]
    public Guid PersonId { get; set; }

    public CombatTaskDetailsKind CombatTaskDetailsKind { get; set; } = CombatTaskDetailsKind.Start;

    public DateOnly EffectiveAt { get; set; } = DateOnly.FromDateTime(DateTime.Now);

    [Required, MaxLength(10, ErrorMessage = "РНОКПП не більше 10 символів.")]
    public string Rnokpp { get; set; } = string.Empty;

    [Required, MaxLength(512, ErrorMessage = "ПІБ не більше 512 символів.")]
    public string FullName { get; set; } = string.Empty;

    public string? Callsign { get; set; }
}
