//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// Order
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

/// <summary>
/// Наказ, що закриває конкретний план. Тримає лише назву документа і тягне дату з плану.
/// </summary>
public class Order
{
    public Guid Id { get; set; }

    /// <summary>План, який закриває цей наказ (1:1).</summary>
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;

    /// <summary>Назва/номер документа (обов’язкова).</summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Дата з плану, яку цей наказ фіксує як «дату початку або кінця».
    /// Значення береться з Plan.PlannedAtUtc відповідно до Plan.TimeKind.
    /// </summary>
    public DateTime EffectiveMomentUtc { get; set; }

    /// <summary>Службові поля аудиту.</summary>
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
}