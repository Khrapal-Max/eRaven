//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDetails
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Snapshot-рядок завдання по людині (частина документа).
/// Це "підстава", з якої табель формує свої факти.
/// </summary>
public sealed class CombatTaskDetails
{
    public Guid Id { get; set; }

    public Guid CombatTaskId { get; set; }
    public CombatTask? CombatTask { get; set; }

    public CombatTaskDetailsKind Kind { get; set; }

    public DateOnly EffectiveAt { get; set; }

    public Guid PersonId { get; set; }

    // Snapshot fields
    public string Rnokpp { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string? Rank { get; set; }
    public string? Position { get; set; }
    public string? Weapon { get; set; }
    public string? Callsign { get; set; }
}
