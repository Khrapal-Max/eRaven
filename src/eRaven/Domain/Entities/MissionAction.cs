//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionAction
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

/// <summary>
/// Запис місії особи, зв'язує особу з документом
/// 
/// Містить назву документа ініціатора.
/// </summary>
public class MissionAction
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int Sequence { get; set; }

    /// <summary>
    /// Назва/номер документа планування (джерело істини).
    /// </summary>
    public string SourceDocNo { get; set; } = string.Empty;

    public ActionKind Action { get; set; }

    public Guid MissionId { get; set; }

    public DateOnly ActionDate { get; set; }
}
