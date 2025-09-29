//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonService
//-----------------------------------------------------------------------------

using eRaven.Application.Projector;
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

        var rank = person.Rank ?? throw new ArgumentException("Rank обов'язковий.", nameof(person));
        var lastName = person.LastName ?? throw new ArgumentException("LastName обов'язкове.", nameof(person));
        var firstName = person.FirstName ?? throw new ArgumentException("FirstName обов'язкове.", nameof(person));
        var bzvp = person.BZVP ?? string.Empty;

        var id = person.Id == Guid.Empty ? Guid.NewGuid() : person.Id;
        var createdUtc = DateTime.UtcNow;

        var aggregate = Person.Create(
            id,
            rnokpp!,
            rank,
            lastName,
            firstName,
            person.MiddleName,
            bzvp,
            person.Weapon,
            person.Callsign,
            person.IsAttached,
            person.AttachedFromUnit,
            createdUtc);

        if (person.PositionUnitId is Guid positionUnitId)
        {
            var position = await db.PositionUnits
                .FirstOrDefaultAsync(p => p.Id == positionUnitId, ct)
                ?? throw new InvalidOperationException("Посада не знайдена.");

            if (!position.IsActived)
            {
                throw new InvalidOperationException("Неможливо призначити на неактивну посаду.");
            }

            var positionBusy = await db.Persons
                .AsNoTracking()
                .AnyAsync(p => p.PositionUnitId == positionUnitId, ct);

            if (positionBusy)
            {
                throw new InvalidOperationException("Посада вже зайнята іншою особою.");
            }

            var assignment = aggregate.AssignToPosition(position, createdUtc, null, "system");
            assignment.Person = aggregate;
            assignment.PositionUnit = position;
        }

        if (person.StatusKindId is int statusKindId)
        {
            var statusKind = await db.StatusKinds
                .FirstOrDefaultAsync(k => k.Id == statusKindId, ct)
                ?? throw new InvalidOperationException("Вказаний статус не існує.");

            var transitions = await db.StatusTransitions
                .AsNoTracking()
                .ToListAsync(ct);

            var status = aggregate.SetStatus(statusKind, createdUtc, null, "system", transitions);
            status.Person = aggregate;
            status.StatusKind = statusKind;
            aggregate.StatusKind = statusKind;
        }

        await PersonAggregateProjector.ProjectAsync(db, aggregate, ct);
        await db.SaveChangesAsync(ct);
        return aggregate;
    }

    // ---------- Update (акуратно з трекінгом) ----------
    public async Task<bool> UpdateAsync(Person person, CancellationToken ct = default)
    {
        await using var db = await _dbf.CreateDbContextAsync(ct);

        ArgumentNullException.ThrowIfNull(person);

        var rnokpp = person.Rnokpp?.Trim();
        if (string.IsNullOrWhiteSpace(rnokpp))
            throw new ArgumentException("RNOKPP обов'язковий.", nameof(person));

        var duplicate = await db.Persons
            .AsNoTracking()
            .AnyAsync(p => p.Id != person.Id && p.Rnokpp == rnokpp, ct);

        if (duplicate)
            throw new InvalidOperationException("Особа з таким РНОКПП вже існує.");

        var aggregate = await db.Persons
            .AsNoTracking()
            .Include(p => p.PositionAssignments)
            .Include(p => p.StatusHistory)
            .Include(p => p.PlanActions)
            .FirstOrDefaultAsync(p => p.Id == person.Id, ct);

        if (aggregate is null)
        {
            return false;
        }

        var rank = person.Rank ?? throw new ArgumentException("Rank обов'язковий.", nameof(person));
        var lastName = person.LastName ?? throw new ArgumentException("LastName обов'язкове.", nameof(person));
        var firstName = person.FirstName ?? throw new ArgumentException("FirstName обов'язкове.", nameof(person));
        var bzvp = person.BZVP ?? string.Empty;

        aggregate.UpdateCard(
            rnokpp!,
            rank,
            lastName,
            firstName,
            person.MiddleName,
            bzvp,
            person.Weapon,
            person.Callsign,
            person.IsAttached,
            person.AttachedFromUnit,
            DateTime.UtcNow);

        await PersonAggregateProjector.ProjectAsync(db, aggregate, ct);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
