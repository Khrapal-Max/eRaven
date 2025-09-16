//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// OrderViewModel
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.OrderViewModels;

public sealed record OrderViewModel(Guid Id, string Name, DateTime EffectiveMomentUtc, string? Author, DateTime RecordedUtc);