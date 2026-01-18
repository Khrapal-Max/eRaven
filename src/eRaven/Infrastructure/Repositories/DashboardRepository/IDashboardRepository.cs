//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IDashboardRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Dashboard;

namespace eRaven.Infrastructure.Repositories.DashboardRepository;

public interface IDashboardRepository
{
    Task<PersonnelDashboardSnapshot> GetPersonnelDashboardAsync(CancellationToken ct = default);
}
