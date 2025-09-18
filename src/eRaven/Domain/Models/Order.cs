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

    /// <summary>Назва/номер наказу (обов’язково).</summary>
    public string Name { get; set; } = default!;

    /// <summary>Час набуття чинності (UTC).</summary>
    public DateTime EffectiveMomentUtc { get; set; }

    /// <summary>Автор/аудит.</summary>
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Плани, що закриваються цим наказом (1:N).</summary>
    public ICollection<Plan> Plans { get; set; } = [];
}