//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskPlanDocument
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Entities;

public sealed class CombatTaskPlanDocument
{
    public Guid Id { get; set; }

    /// <summary>Дата запису (коли внесли документ).</summary>
    public DateOnly RecordedAt { get; set; }

    /// <summary>Дата планування (на який день/період план).</summary>
    public DateOnly PlanningDate { get; set; }

    /// <summary>Назва/номер документа планування.</summary>
    public string PlanningDocTitle { get; set; } = string.Empty;

    public CombatTaskPlanDocumentStatus Status { get; set; } = CombatTaskPlanDocumentStatus.Draft;

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public string? CanceledReason { get; set; }
    public string? CanceledBy { get; set; }
    public DateTime? CanceledAtUtc { get; set; }

    public List<CombatTaskPlanLine> Lines { get; set; } = [];
}
