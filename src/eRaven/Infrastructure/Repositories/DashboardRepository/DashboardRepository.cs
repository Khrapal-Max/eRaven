//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DashboardRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.DashboardRepository;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.DashboardRepository;

public sealed class DashboardRepository(
    IDbContextFactory<AppDbContext> dbFactory)
    : IDashboardRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonReadModel>> GetPersonnelDashboardAsync(
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        // В табелі = тільки Lifecycle == Enrolled
        return await db.PersonRead
            .AsNoTracking()
            .Where(x => x.Lifecycle == PersonLifecycle.Enrolled)
            .ToListAsync(ct);
    }
}