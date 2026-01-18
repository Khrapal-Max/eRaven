//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonnelDashboardQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Dashboard;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Dashboard;
using eRaven.Infrastructure.Repositories.DashboardRepository;

namespace eRaven.Application.Handlers.Dashboard;

public sealed class GetPersonnelDashboardQueryHandler(
    IDashboardRepository repo)
    : IQueryHandler<GetPersonnelDashboardQuery, PersonnelDashboardDto>
{
    private readonly IDashboardRepository _repo = repo;

    public async Task<PersonnelDashboardDto> HandleAsync(
        GetPersonnelDashboardQuery query,
        CancellationToken ct = default)
    {
        var snap = await _repo.GetPersonnelDashboardAsync(ct);

        return new PersonnelDashboardDto(
            TotalInTimesheet: snap.TotalInTimesheet,
            TimesheetUnit: snap.TimesheetUnit,
            TimesheetOrder: snap.TimesheetOrder,
            TimesheetBr: snap.TimesheetBr,
            GeneratedAtUtc: DateTime.UtcNow
        );
    }
}