//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonService
//-----------------------------------------------------------------------------

using eRaven.Domain.Person;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eRaven.Application.Services.PersonService;

public class PersonService(IDbContextFactory<AppDbContext> dbf) : IPersonService
{
    private readonly IDbContextFactory<AppDbContext> _dbf = dbf;

    // ---------- Search (легкий, без історій) ----------

    public async Task<IReadOnlyList<Person>> SearchAsync(Expression<Func<Person, bool>>? predicate, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var q = db.Persons
       .AsNoTracking()
       .Include(p => p.StatusKind)
       .Include(p => p.PositionUnit)
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
        await using var db = await _dbf.CreateDbContextAsync(ct);

        return await db.Persons
            .AsNoTracking()
            .Include(p => p.StatusKind)
            .Include(p => p.PositionUnit)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    // ---------- Create ----------
    public async Task<Person> CreateAsync(Person person, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        if (string.IsNullOrWhiteSpace(person.Rnokpp))
            throw new ArgumentException("RNOKPP обов'язковий.", nameof(person));

        // ручна перевірка унікальності (краще повідомлення ніж SQL-виняток)
        var exists = await db.Persons.AnyAsync(p => p.Rnokpp == person.Rnokpp, ct);
        if (exists) throw new InvalidOperationException("Особа з таким РНОКПП вже існує.");

        person.Id = person.Id == Guid.Empty ? Guid.NewGuid() : person.Id;
        person.CreatedUtc = DateTime.UtcNow;
        person.ModifiedUtc = person.CreatedUtc;

        db.Persons.Add(person);
        await db.SaveChangesAsync(ct);
        return person;
    }

    // ---------- Update (акуратно з трекінгом) ----------
    public async Task<bool> UpdateAsync(Person person, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        var current = await db.Persons
            .FirstOrDefaultAsync(p => p.Id == person.Id, ct);

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

        // IsAttached / AttachedFromUnit — теж з картки:
        current.IsAttached = person.IsAttached;
        current.AttachedFromUnit = person.AttachedFromUnit;

        // посаду/статус **тут не змінюємо** (за твоєю домовленістю)

        current.ModifiedUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }
}
