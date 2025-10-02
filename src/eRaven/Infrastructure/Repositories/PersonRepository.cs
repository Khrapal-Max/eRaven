//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Repositories;
using eRaven.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eRaven.Infrastructure.Repositories;

public class PersonRepository(AppDbContext context) : IPersonRepository
{
    private readonly AppDbContext _context = context;

    public async Task<PersonAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Persons
            .Include(p => p.StatusHistory.Where(s => s.IsActive))
            .Include(p => p.PositionAssignments)
            .Include(p => p.PlanActions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<PersonAggregate?> GetByRnokppAsync(string rnokpp, CancellationToken ct = default)
    {
        return await _context.Persons
            .Include(p => p.StatusHistory.Where(s => s.IsActive))
            .FirstOrDefaultAsync(p => p.PersonalInfo.Rnokpp == rnokpp, ct);
    }

    public async Task<IReadOnlyList<PersonAggregate>> SearchAsync(
        Expression<Func<PersonAggregate, bool>>? predicate,
        CancellationToken ct = default)
    {
        var query = _context.Persons
            .Include(p => p.StatusHistory.Where(s => s.IsActive))
            .Include(p => p.PositionAssignments)
            .AsQueryable();

        if (predicate != null)
            query = query.Where(predicate);

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(PersonAggregate person, CancellationToken ct = default)
    {
        await _context.Persons.AddAsync(person, ct);
        await _context.SaveChangesAsync(ct);

        // Публікуємо Domain Events
        await PublishDomainEventsAsync(person);
    }

    public async Task UpdateAsync(PersonAggregate person, CancellationToken ct = default)
    {
        _context.Persons.Update(person);
        await _context.SaveChangesAsync(ct);

        // Публікуємо Domain Events
        await PublishDomainEventsAsync(person);
    }

    public bool IsPositionOccupied(Guid positionUnitId)
    {
        return _context.Persons.Any(p => p.PositionUnitId == positionUnitId);
    }

    private async Task PublishDomainEventsAsync(PersonAggregate person)
    {
        // Тут можна використати MediatR або інший медіатор
        var events = person.DomainEvents.ToList();
        person.ClearDomainEvents();

        foreach (var @event in events)
        {
            // await _mediator.Publish(@event);
        }

        await Task.CompletedTask;
    }
}