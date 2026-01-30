//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskEntry
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Денормалізований "рядок" документа.
/// 
/// Концепція:
/// - В житті один документ А може містити: місію + дію + дату + список осіб.
/// - В системі це зберігаємо як "ГРУПУ" (GroupId) з багатьма рядками (по одному на людину).
///
/// Чому так:
/// - UI читає документ без JOIN-ів (все вже є в цьому рядку).
/// - "Документ не пливе": снапшот місії + снапшот особи фіксуються на момент введення.
/// </summary>
public sealed class CombatTaskEntry
{
    public Guid Id { get; set; }

    /// <summary>
    /// FK на документ (B).
    /// </summary>
    public Guid DocumentId { get; set; }
    public CombatTaskDocument? Document { get; set; }

    /// <summary>
    /// Ідентифікатор групи (умовний "рядок як в житті": місія/дія/дата + багато осіб).
    /// У межах одного документа GroupId об'єднує кілька записів (по 1 на особу).
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// Порядок груп у документі (для UI/друку).
    /// </summary>
    public int GroupSequence { get; set; }

    /// <summary>
    /// Номер документа-джерела (документ А / рапорт), який користувач вводить вручну.
    /// </summary>
    public string SourceDocNo { get; set; } = string.Empty;

    /// <summary>
    /// Тип дії: Start / End.
    /// </summary>
    public ActionKind Action { get; set; }

    /// <summary>
    /// Дата дії (без часу).
    /// </summary>
    public DateOnly ActionDate { get; set; }

    /// <summary>
    /// Ідентифікатор місії (для фільтрів/зв’язку, навіть якщо місія потім закривається).
    /// </summary>
    public Guid MissionId { get; set; }

    /// <summary>
    /// Снапшот відображення місії (для UI без JOIN).
    /// </summary>
    public string MissionDisplaySnapshot { get; set; } = string.Empty;

    /// <summary>
    /// Ідентифікатор особи.
    /// </summary>
    public Guid PersonId { get; set; }

    // ---- Snapshot особи (фіксуємо на момент внесення групи) ----
    public string RNOKPP { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Weapon { get; set; } = string.Empty;
    public string Callsign { get; set; } = string.Empty;
}
