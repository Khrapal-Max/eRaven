//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PublishDailyOrderViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.OrderViewModels;

public sealed record CreatePublishDailyOrderViewModel(
    string Name,                       // № наказу
    DateTime EffectiveMomentUtc,       // UTC
    string? Author,
    IReadOnlyList<Guid> PlanIds,       // які плани закрити
    IReadOnlyList<Guid>? IncludePlanActionIds = null, // які дії підтвердити (null => всі в обраних планах)
    bool AutoReturnForOpenDispatch = true             // авто-Return там, де остання дія в плані = Dispatch
);