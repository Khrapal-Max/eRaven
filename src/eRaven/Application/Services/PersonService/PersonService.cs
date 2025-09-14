//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonService
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eRaven.Application.Services.PersonService;

public class PersonService(AppDbContext appDbContext) : IPersonService
{
    private readonly AppDbContext _appDbContext = appDbContext;

    // ---------- Search (легкий, без історій) ----------

    public async Task<IReadOnlyList<Person>> SearchAsync(Expression<Func<Person, bool>>? predicate, CancellationToken ct = default)
    {
        var q = _appDbContext.Persons
       .AsNoTracking()
       .Include(p => p.StatusKind)
       .Include(p => p.PositionUnit)
       .Include(p => p.PersonPlanning)
       .AsQueryable();

        if (predicate is not null)
            q = q.Where(predicate);

        var response = await q.OrderBy(x => x.LastName)
             .ThenBy(x => x.FirstName)
             .ThenBy(x => x.MiddleName)
             .ToListAsync(ct);

        return response.AsReadOnly();
    }

    // ---------- GetById (для картки; без історій поки) ----------
    public async Task<Person?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _appDbContext.Persons
            .AsNoTracking()
            .Include(p => p.StatusKind)
            .Include(p => p.PositionUnit)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    // ---------- Create ----------
    public async Task<Person> CreateAsync(Person person, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(person.Rnokpp))
            throw new ArgumentException("RNOKPP обов'язковий.", nameof(person));

        // ручна перевірка унікальності (краще повідомлення ніж SQL-виняток)
        var exists = await _appDbContext.Persons.AnyAsync(p => p.Rnokpp == person.Rnokpp, ct);
        if (exists) throw new InvalidOperationException("Особа з таким РНОКПП вже існує.");

        person.Id = person.Id == Guid.Empty ? Guid.NewGuid() : person.Id;
        person.CreatedUtc = DateTime.UtcNow;
        person.ModifiedUtc = person.CreatedUtc;

        _appDbContext.Persons.Add(person);
        await _appDbContext.SaveChangesAsync(ct);
        return person;
    }

    // ---------- Update (акуратно з трекінгом) ----------
    public async Task<bool> UpdateAsync(Person person, CancellationToken ct = default)
    {
        var current = await _appDbContext.Persons.FirstOrDefaultAsync(p => p.Id == person.Id, ct);
        if (current is null) return false;

        // поля, які дозволено редагувати у картці
        current.LastName = person.LastName;
        current.FirstName = person.FirstName;
        current.MiddleName = person.MiddleName;
        current.Rnokpp = person.Rnokpp;
        current.Rank = person.Rank;
        current.Callsign = person.Callsign;
        current.BZVP = person.BZVP;
        current.Weapon = person.Weapon;
        current.PersonPlanning = person.PersonPlanning;

        // IsAttached / AttachedFromUnit — теж з картки:
        current.IsAttached = person.IsAttached;
        current.AttachedFromUnit = person.AttachedFromUnit;

        // посаду/статус **тут не змінюємо** (за твоєю домовленістю)

        current.ModifiedUtc = DateTime.UtcNow;

        await _appDbContext.SaveChangesAsync(ct);
        return true;
    }
}
