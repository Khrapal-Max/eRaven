//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanParticipantAction
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Domain.Models;

public class PlanParticipantAction
{
    public Guid Id { get; set; }

    public Guid PlanParticipantId { get; set; }
    public PlanParticipant PlanParticipant { get; set; } = default!;

    // Денормалізація для швидких звітів
    public Guid PlanId { get; set; }
    public Guid PersonId { get; set; }

    public PlanActionType ActionType { get; set; }   // Dispatch / Return
    public DateTime EventAtUtc { get; set; }         // Дата події

    // Контекст дії
    public string Location { get; set; } = default!;
    public string GroupName { get; set; } = default!;
    public string CrewName { get; set; } = default!;

    public string? Note { get; set; }
    public string? Author { get; set; }
    public DateTime RecordedUtc { get; set; } = DateTime.UtcNow;
}
