//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanViewModel
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Application.ViewModels.PlanViewModels;

public sealed record PlanViewModel(
    Guid Id,
    string PlanNumber,
    PlanState State,
    string? Author,
    DateTime RecordedUtc,
    Guid? OrderId,
    string? OrderName
);