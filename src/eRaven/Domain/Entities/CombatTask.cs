//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTask
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Entities;

/// <summary>
/// Групування рядків у межах документа по місії.
/// </summary>
public sealed class CombatTask
{
    public Guid Id { get; set; }

    public Guid CombatTaskDocumentId { get; set; }
    public CombatTaskDocument? CombatTaskDocument { get; set; }

    public Guid MissionId { get; set; }
    public Mission? Mission { get; set; }

    public string SourceDocument { get; set; } = string.Empty;

    public ICollection<CombatTaskDetails> CombatTaskDetails { get; set; } = [];
}