//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionModel
//-----------------------------------------------------------------------------

using System.ComponentModel.DataAnnotations;

namespace eRaven.Application.DTOs.Timesheet.Models;

//======================================================================
// Form model (minimal validation)
//======================================================================

/// <summary>
/// Модель форми для переходу стану.
/// Валідація мінімальна: InputDate і NextCode обов’язкові.
/// </summary>
public sealed class TransitionModel
{
    /// <summary>Цільова особа.</summary>
    public Guid PersonId { get; set; }

    /// <summary>Дата, на яку визначається поточний активний стан (anchor).</summary>
    public DateOnly AnchorDate { get; set; }

    /// <summary>
    /// Дата, яку вводить користувач (“по/закінчення”).
    /// Трактування залежить від EndDateMeaning поточного коду.
    /// </summary>
    [Required]
    public DateOnly InputDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    /// <summary>Наступний код (має бути дозволений політикою).</summary>
    [Required(ErrorMessage = "Оберіть наступний код.")]
    public string NextCode { get; set; } = string.Empty;

    /// <summary>Опційний референс/підстава.</summary>
    public string? Reference { get; set; }

    /// <summary>Опційна примітка.</summary>
    public string? Note { get; set; }
}