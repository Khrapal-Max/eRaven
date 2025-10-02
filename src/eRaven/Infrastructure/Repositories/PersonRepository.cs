//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Repositories;
using eRaven.Domain.Aggregates;
using eRaven.Domain.Events;
using eRaven.Domain.Events.Integrations;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace eRaven.Infrastructure.Repositories;

public class PersonRepository(AppDbContext context) : IPersonRepository
{
    private readonly AppDbContext _context = context;

    public async Task<PersonAggregate?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Persons
            .Include(p => p.StatusHistory)
            .Include(p => p.PositionHistory)
            .Include(p => p.PlanActions)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<PersonAggregate?> GetByRnokppAsync(string rnokpp, CancellationToken ct = default)
    {
        return await _context.Persons
            .Include(p => p.StatusHistory)
            .Include(p => p.PositionHistory)
            .Include(p => p.PlanActions)
            .FirstOrDefaultAsync(p => p.PersonalInfo.Rnokpp == rnokpp, ct);
    }

    public async Task<IReadOnlyList<PersonAggregate>> SearchAsync(
        Expression<Func<PersonAggregate, bool>>? predicate,
        CancellationToken ct = default)
    {
        var query = _context.Persons
            .Include(p => p.StatusHistory)
            .Include(p => p.PositionHistory)
            .AsQueryable();

        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.ToListAsync(ct);
    }

    public async Task AddAsync(PersonAggregate person, CancellationToken ct = default)
    {
        await _context.Persons.AddAsync(person, ct);
        await _context.SaveChangesAsync(ct);
        await PublishDomainEventsAsync(person, ct);
    }

    public async Task UpdateAsync(PersonAggregate person, CancellationToken ct = default)
    {
        _context.Persons.Update(person);
        await _context.SaveChangesAsync(ct);
        await PublishDomainEventsAsync(person, ct);
    }

    public bool IsPositionOccupied(Guid positionUnitId)
    {
        return _context.Persons.Any(p => p.CurrentPositionUnitId == positionUnitId);
    }

    private async Task PublishDomainEventsAsync(PersonAggregate person, CancellationToken ct)
    {
        var events = person.DomainEvents.ToList();
        person.ClearDomainEvents();

        foreach (var @event in events)
        {
            await HandleDomainEventAsync(@event, ct);
        }
    }

    private async Task HandleDomainEventAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        switch (domainEvent)
        {
            case PersonCreatedDomainEvent e:
                await HandlePersonCreatedAsync(e, ct);
                break;

            case PersonStatusChangedDomainEvent e:
                await HandlePersonStatusChangedAsync(e, ct);
                break;

            case PersonAssignedToPositionDomainEvent e:
                await HandlePersonAssignedToPositionAsync(e, ct);
                break;

            case PersonUnassignedFromPositionDomainEvent e:
                await HandlePersonUnassignedFromPositionAsync(e, ct);
                break;

            case PlanActionCreatedDomainEvent e:
                await HandlePlanActionCreatedAsync(e, ct);
                break;
        }
    }

    // Хендлери для кожного типу події
    private async Task HandlePersonCreatedAsync(PersonCreatedDomainEvent e, CancellationToken ct)
    {
        // TODO: Логіка обробки створення особи (логування, нотифікація тощо)
        await Task.CompletedTask;
    }

    private async Task HandlePersonStatusChangedAsync(PersonStatusChangedDomainEvent e, CancellationToken ct)
    {
        // TODO: Логіка обробки зміни статусу
        await Task.CompletedTask;
    }

    private async Task HandlePersonAssignedToPositionAsync(PersonAssignedToPositionDomainEvent e, CancellationToken ct)
    {
        // TODO: Логіка обробки призначення на посаду
        await Task.CompletedTask;
    }

    private async Task HandlePersonUnassignedFromPositionAsync(PersonUnassignedFromPositionDomainEvent e, CancellationToken ct)
    {
        // TODO: Логіка обробки зняття з посади
        await Task.CompletedTask;
    }

    private async Task HandlePlanActionCreatedAsync(PlanActionCreatedDomainEvent e, CancellationToken ct)
    {
        // TODO: Логіка обробки створення планової дії
        await Task.CompletedTask;
    }
}
