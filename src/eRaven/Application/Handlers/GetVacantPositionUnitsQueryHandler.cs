//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetVacantPositionUnitsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Handlers;

public sealed class GetVacantPositionUnitsQueryHandler(IDbContextFactory<AppDbContext> dbFactory)
    : IQueryHandler<GetVacantPositionUnitsQuery, IReadOnlyList<PositionUnitOptionDto>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<PositionUnitOptionDto>> HandleAsync(GetVacantPositionUnitsQuery query, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.PositionUnits
            .AsNoTracking()
            .Where(x => x.IsActived)
            .Where(x => x.State == PositionUnitState.Vacant);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(x =>
                x.Code.Contains(s) ||
                x.ShortName.Contains(s) ||
                x.FullName.Contains(s));
        }

        var list = await q
            .OrderBy(x => x.Number)
            .Take(query.Take)
            .Select(x => new PositionUnitOptionDto(
                x.Id,
                x.Code,
                x.ShortName,
                x.FullName,
                x.Rank,
                x.Tarif
            ))
            .ToListAsync(ct);

        return list;
    }
}