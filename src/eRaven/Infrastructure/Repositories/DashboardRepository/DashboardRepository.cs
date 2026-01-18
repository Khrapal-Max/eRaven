//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DashboardRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Dashboard;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.DashboardRepository;

public sealed class DashboardRepository(
    IDbContextFactory<AppDbContext> dbFactory)
    : IDashboardRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<PersonnelDashboardSnapshot> GetPersonnelDashboardAsync(
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // В табелі = тільки Lifecycle == Enrolled
        var enrolled = db.PersonRead
            .AsNoTracking()
            .Where(x => x.Lifecycle == PersonLifecycle.Enrolled);

        var slice = await enrolled
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Unit = g.Count(x => x.EnrollmentKind == EnrollmentKind.Unit),
                Order = g.Count(x => x.EnrollmentKind == EnrollmentKind.AttachedByList),
                Br = g.Count(x => x.EnrollmentKind == EnrollmentKind.AttachedByOrder),
            })
            .FirstOrDefaultAsync(ct);

        return new PersonnelDashboardSnapshot(
            TotalInTimesheet: slice?.Total ?? 0,
            TimesheetUnit: slice?.Unit ?? 0,
            TimesheetOrder: slice?.Order ?? 0,
            TimesheetBr: slice?.Br ?? 0
        );
    }
}