//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IPersonService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PersonService;

public class PersonService(AppDbContext appDbContext) : IPersonService
{
    private readonly AppDbContext _appDbContext = appDbContext;

    public async Task<IReadOnlyList<Person>> SearchAsync(string? query, CancellationToken ct = default)
    {
        return await _appDbContext.Persons
           .AsNoTracking()
           .Where(x => query == null
                       || x.Rnokpp.Contains(query)
                       || x.LastName.Contains(query)
                       || x.FirstName.Contains(query)
                       || (x.MiddleName != null && x.MiddleName.Contains(query))
                       || (x.Rank != null && x.Rank.Contains(query))
                       || (x.BZVP != null && x.BZVP.Contains(query))
                       || (x.Callsign != null && x.Callsign.Contains(query))
                       || (x.Weapon != null && x.Weapon.Contains(query)))
           .Include(x => x.StatusHistory)
           .Include(x => x.PositionAssignments)
           .ToListAsync(ct);
    }

    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _appDbContext.Persons.AsNoTracking()
            .Include(x => x.StatusHistory)
            .Include(x => x.PositionAssignments)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<Person> CreateAsync(Person person, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(person.Rnokpp);

        var card = _appDbContext.Persons.Add(person).Entity;

        await _appDbContext.SaveChangesAsync(ct);

        return card;
    }

    public async Task<bool> UpdateAsync(Person person, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(person.Rnokpp);

        var card = await _appDbContext.Persons.AsNoTracking()
           .FirstOrDefaultAsync(x => x.Id == person.Id, ct);

        card = person;

        _appDbContext.Persons.Update(card);

        await _appDbContext.SaveChangesAsync(ct);

        return true;
    }
}
