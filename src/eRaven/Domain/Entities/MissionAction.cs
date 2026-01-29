//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAction
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Дія в документі (початок/кінець виконання місії).
/// Є "верхнім" елементом документа та єдиним джерелом порядку через <see cref="Sequence"/>.
/// </summary>
public sealed class MissionAction
{
    public Guid Id { get; set; }

    /// <summary>
    /// Документ, в межах якого сформовано дію.
    /// </summary>
    public Guid DocumentId { get; set; }

    /// <summary>
    /// Порядок дій у документі (єдине джерело істини щодо послідовності).
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Назва/номер документа-ініціатора (джерело істини "з життя", напр. рапорт/план А).
    /// </summary>
    public string SourceDocNo { get; set; } = string.Empty;

    /// <summary>
    /// Тип дії: Start/End.
    /// </summary>
    public ActionKind Action { get; set; }

    /// <summary>
    /// Місія, по якій виконується дія.
    /// </summary>
    public Guid MissionId { get; set; }

    /// <summary>
    /// Дата дії (DateOnly — без часу).
    /// </summary>
    public DateOnly ActionDate { get; set; }
}
