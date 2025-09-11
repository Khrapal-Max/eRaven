//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreatePlanCreateViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Domain.Models;

public class PlanServiceOptions
{
    public int Id { get; set; }                          // завжди 1 (один-єдиний рядок)

    public int? DispatchStatusKindId { get; set; }       // цільовий статус для Dispatch (наприклад, "В БР")
    public int? ReturnStatusKindId { get; set; }         // цільовий статус для Return (наприклад, "В районі")

    public StatusKind? DispatchStatusKind { get; set; }  // навігації (зручно для джойнів)
    public StatusKind? ReturnStatusKind { get; set; }

    public string? Author { get; set; }
    public DateTime ModifiedUtc { get; set; }
}