// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// AddPlanElementsModal (працює з PersonPlanInfo; автоконтекст Return)
// -----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Components.Pages.Plans.Modals;

public sealed partial class AddPlanElementsModal
{
    // Payload до батька
    public sealed class AddElementsPayload
    {
        public PlanType Type { get; init; }
        public DateOnly LocalDate { get; init; }
        public int Hour { get; init; }
        public int Minute { get; init; }
        public string? Location { get; init; }
        public string? Group { get; init; }
        public string? Tool { get; init; }
        public string? Note { get; init; }
        public IReadOnlyCollection<Guid> PersonIds { get; init; } = [];
    }
}
