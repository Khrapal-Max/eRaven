//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SearchPersonsQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Application.Queries.Persons;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.QueryHandlers.Persons;

public sealed class SearchPersonsQueryHandler(IDbContextFactory<AppDbContext> dbFactory)
        : IQueryHandler<SearchPersonsQuery, IReadOnlyList<PersonDto>>
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;

    public async Task<IReadOnlyList<PersonDto>> HandleAsync(
        SearchPersonsQuery query,
        CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        var q = db.Persons.AsNoTracking();

        if (query.Predicate != null)
            q = q.Where(query.Predicate);

        var persons = await q
            .OrderBy(p => p.PersonalInfo.LastName)
            .ThenBy(p => p.PersonalInfo.FirstName)
            .Select(p => new PersonDto
            {
                Id = p.Id,
                Rnokpp = p.PersonalInfo.Rnokpp,
                LastName = p.PersonalInfo.LastName,
                FirstName = p.PersonalInfo.FirstName,
                MiddleName = p.PersonalInfo.MiddleName,
                FullName = p.PersonalInfo.LastName + " " + p.PersonalInfo.FirstName +
                          (p.PersonalInfo.MiddleName != null ? " " + p.PersonalInfo.MiddleName : ""),
                Rank = p.MilitaryDetails.Rank,
                BZVP = p.MilitaryDetails.BZVP,
                Weapon = p.MilitaryDetails.Weapon,
                Callsign = p.MilitaryDetails.Callsign,
                StatusKindId = p.StatusKindId,
                PositionUnitId = p.PositionUnitId,
                CreatedUtc = p.CreatedUtc,
                ModifiedUtc = p.ModifiedUtc
            })
            .ToListAsync(ct);

        return persons;
    }
}
