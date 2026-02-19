//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetPersonnelDashboardQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.DashboardRepository;
using eRaven.Application.DTOs.Dashboard;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Dashboard;
using eRaven.Domain.Enums;

namespace eRaven.Application.Handlers.Dashboard;

/// <summary>
/// Повертає дані по кількості людей по штату, наказу та розпорядженню.
/// </summary>
public sealed class GetPersonnelDashboardQueryHandler(
    IDashboardRepository repo)
    : IQueryHandler<GetPersonnelDashboardQuery, PersonnelDashboardDto>
{
    private readonly IDashboardRepository _repo = repo;

    /// <inheritdoc />
    public async Task<PersonnelDashboardDto> HandleAsync(GetPersonnelDashboardQuery query,
        CancellationToken ct = default)
    {
        var persons = await _repo.GetPersonnelDashboardAsync(ct);

        var total = persons.Count;
        var unit = persons.Count(x => x.EnrollmentKind == EnrollmentKind.Unit);
        var byList = persons.Count(x => x.EnrollmentKind == EnrollmentKind.AttachedByList);
        var byOrder = persons.Count(x => x.EnrollmentKind == EnrollmentKind.AttachedByOrder);

        return new PersonnelDashboardDto(
            TotalInTimesheet: total,
            TimesheetUnit: unit,
            TimesheetOrder: byList,
            TimesheetBr: byOrder,
            GeneratedAtUtc: DateTime.UtcNow);
    }
}