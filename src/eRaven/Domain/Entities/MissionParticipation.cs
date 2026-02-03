//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionParticipation
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Участь особи в місії як інтервал [From..To].
///
/// Принципи:
/// - Один активний інтервал на особу (To = null, IsVoided = false) — інваріант.
/// - Для звітів на день/місяць використовується overlap-умова:
///   From <= to AND (To IS NULL OR To >= from).
/// - Snapshot полів (місія + особа) дозволяє робити UI/звіти без JOIN-ів.
/// </summary>
public sealed class MissionParticipation
{
    /// <summary>PK рядка участі.</summary>
    public Guid Id { get; set; }

    //======================================================================
    // Document binding
    //======================================================================

    /// <summary>FK на документ-джерело.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>Навігація на документ.</summary>
    public CombatTaskDocument? Document { get; set; }

    /// <summary>
    /// Ідентифікатор групи (аналог “рядка як в житті”: місія/опис/дата + список осіб).
    /// Використовується для UI-групування.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>Порядок груп у документі (для UI/друку).</summary>
    public int GroupSequence { get; set; }

    /// <summary>
    /// Номер документа-джерела (рапорт/наказ), який вводить користувач вручну.
    /// </summary>
    public string SourceDocNo { get; set; } = string.Empty;

    /// <summary>
    /// Номер документа-джерела (рапорт/наказ), який вводить користувач вручну.
    /// </summary>
    public string? EndSourceDocNo { get; set; }

    //======================================================================
    // Mission snapshot
    //======================================================================

    /// <summary>Ідентифікатор місії.</summary>
    public Guid MissionId { get; set; }

    /// <summary>Снапшот відображення місії для UI/звіту.</summary>
    public string MissionDisplaySnapshot { get; set; } = string.Empty;

    //======================================================================
    // Person snapshot
    //======================================================================

    /// <summary>Ідентифікатор особи.</summary>
    public Guid PersonId { get; set; }

    /// <summary>РНОКПП (снапшот).</summary>
    public string RNOKPP { get; set; } = string.Empty;

    /// <summary>ПІБ (снапшот).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Звання (снапшот).</summary>
    public string Rank { get; set; } = string.Empty;

    /// <summary>Посада (снапшот).</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>Озброєння (снапшот).</summary>
    public string Weapon { get; set; } = string.Empty;

    /// <summary>Позивний (снапшот).</summary>
    public string Callsign { get; set; } = string.Empty;

    //======================================================================
    // Interval state
    //======================================================================

    /// <summary>Початок участі (inclusive).</summary>
    public DateOnly From { get; set; }

    /// <summary>Кінець участі (inclusive). Null = активна участь.</summary>
    public DateOnly? To { get; set; }

    //======================================================================
    // Audit
    //======================================================================

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
