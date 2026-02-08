//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskDetails
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Рядок документа бойового завдання: подія Start/End для конкретної особи на дату.
/// Саме рядки можуть бути в різних документах (Start в одному, End в іншому).
/// </summary>
public sealed class CombatTaskDetails
{
    public Guid Id { get; set; }

    public Guid CombatTaskId { get; set; }
    public CombatTask? CombatTask { get; set; }

    /// <summary>Тип рядка: Start або End.</summary>
    public CombatTaskDetailsKind Kind { get; set; }

    /// <summary>Дата ефекту (коли стартує або закінчує).</summary>
    public DateOnly EffectiveAt { get; set; }

    /// <summary>Посилання на доменну особу.</summary>
    public Guid PersonId { get; set; }

    /// <summary>РНОКПП як snapshot.</summary>
    public string Rnokpp { get; set; } = string.Empty;

    /// <summary>ПІБ як snapshot.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Позивний як snapshot.</summary>
    public string? Callsign { get; set; }
}