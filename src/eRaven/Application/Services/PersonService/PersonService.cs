//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonService
//-----------------------------------------------------------------------------

using eRaven.Application.Services.Shared;
using eRaven.Domain.Models;
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

        ArgumentNullException.ThrowIfNull(person);

        var rnokpp = person.Rnokpp?.Trim();
        if (string.IsNullOrWhiteSpace(rnokpp))
            throw new ArgumentException("RNOKPP обов'язковий.", nameof(person));

        // ручна перевірка унікальності (краще повідомлення ніж SQL-виняток)
        var exists = await db.Persons.AnyAsync(p => p.Rnokpp == rnokpp, ct);
        if (exists) throw new InvalidOperationException("Особа з таким РНОКПП вже існує.");

        var id = person.Id == Guid.Empty ? Guid.NewGuid() : person.Id;
        var createdUtc = DateTime.UtcNow;

        var aggregate = Person.Create(
            id,
            rnokpp!,
            person.Rank ?? string.Empty,
            person.LastName ?? string.Empty,
            person.FirstName ?? string.Empty,
            person.MiddleName,
            person.BZVP ?? string.Empty,
            person.Weapon,
            person.Callsign,
            person.IsAttached,
            person.AttachedFromUnit,
            createdUtc);

        aggregate.StatusKindId = person.StatusKindId;
        aggregate.PositionUnitId = person.PositionUnitId;

        await PersonAggregateProjector.ProjectAsync(db, aggregate, ct);
        await db.SaveChangesAsync(ct);
        return aggregate;
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
