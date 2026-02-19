//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.Timesheets.Models;

/// <summary>
/// Модель форми для переходу стану.
/// Валідація мінімальна: InputDate і CodeId обов’язкові.
/// </summary>
public sealed class TransitionModel
{
    /// <summary>
    /// Дата, яку вводить користувач (“по/закінчення”).
    /// Трактування залежить від StartShiftDays/політики поточного коду.
    /// </summary>
    [Required]
    public DateOnly InputDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Id наступного коду (має бути дозволений політикою).</summary>
    [Required(ErrorMessage = "Оберіть наступний код.")]
    public Guid? CodeId { get; set; }

    /// <summary>Опційний референс/підстава.</summary>
    public string? Reference { get; set; }

    /// <summary>Опційна примітка.</summary>
    public string? Note { get; set; }
}
