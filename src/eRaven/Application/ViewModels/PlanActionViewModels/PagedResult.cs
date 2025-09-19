//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PagedResult
//-----------------------------------------------------------------------------

namespace eRaven.Application.ViewModels.PlanActionViewModels;

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);