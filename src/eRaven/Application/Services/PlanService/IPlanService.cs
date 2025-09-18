//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPlanService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Application.Services.PlanService;

public interface IPlanService
{
    Task<List<Plan>> GetAllAsync(CancellationToken ct = default);

    Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct = default);

    Task<Plan> CreateAsync(string name, string? author = null, CancellationToken ct = default);

    Task<bool> DeleteAsync(Guid planId, CancellationToken ct = default);

    Task<bool> SetApprovedAsync(Guid planId, CancellationToken ct = default); // Позначає як погоджений
}