//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PublishDailyOrderViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.OrderViewModels;

public sealed record class CreatePublishDailyOrderViewModel
{
    public string Name { get; set; } = string.Empty;           // № наказу
    public DateTime EffectiveMomentUtc { get; set; } = DateTime.UtcNow; // UTC
    public string? Author { get; set; }

    // з формою зручніше працювати зі змінюваним списком
    public List<Guid> PlanIds { get; set; } = new();

    // можна залишити null або порожній список — бек усе прийме
    public List<Guid>? IncludePlanActionIds { get; set; }

    public bool AutoReturnForOpenDispatch { get; set; } = true;
}