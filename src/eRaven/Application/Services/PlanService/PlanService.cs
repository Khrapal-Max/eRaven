/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanService;

public sealed class PlanService(AppDbContext db) : IPlanService
{
    private readonly AppDbContext _db = db;

    // -------------------------------- Queries --------------------------------

    public async Task<IEnumerable<Plan>> GetAllPlansAsync(CancellationToken ct = default)
        => await _db.Plans.AsNoTracking()
            .Include(p => p.Participants)
            .OrderByDescending(p => p.PlannedAtUtc)
            .ToListAsync(ct);

    public async Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct = default)
        => await _db.Plans.AsNoTracking()
            .Include(p => p.Participants)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

    // -------------------------------- Create ---------------------------------

    public async Task<Plan> CreateAsync(CreatePlanCreateViewModel vm, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vm);
        if (string.IsNullOrWhiteSpace(vm.PlanNumber))
            throw new ArgumentException("PlanNumber обовʼязковий.", nameof(vm));

        var plannedUtc = NormalizeToUtc(vm.PlannedAt);

        var personIds = (vm.ParticipantIds ?? [])
            .Where(x => x != Guid.Empty)
            .Distinct()
            .ToArray();

        if (personIds.Length == 0)
            throw new InvalidOperationException("План має містити принаймні одного учасника.");

        // Тягнемо людей для побудови снапшотів (без правил переходів)
        var persons = await _db.Persons
            .Include(p => p.PositionUnit)
            .Include(p => p.StatusKind)
            .Where(p => personIds.Contains(p.Id))
            .ToListAsync(ct);

        if (persons.Count != personIds.Length)
            throw new InvalidOperationException("Деякі учасники не знайдені.");

        var snapshots = persons.Select(p => BuildSnapshot(Guid.Empty, p, vm.Author)).ToList();

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = vm.PlanNumber.Trim(),
            Type = vm.Type,
            PlannedAtUtc = plannedUtc,
            TimeKind = vm.TimeKind,
            Location = string.IsNullOrWhiteSpace(vm.Location) ? null : vm.Location.Trim(),
            GroupName = string.IsNullOrWhiteSpace(vm.GroupName) ? null : vm.GroupName.Trim(),
            ToolType = string.IsNullOrWhiteSpace(vm.ToolType) ? null : vm.ToolType.Trim(),
            TaskDescription = string.IsNullOrWhiteSpace(vm.TaskDescription) ? null : vm.TaskDescription.Trim(),
            Participants = snapshots,
            State = PlanState.Open,
            Author = vm.Author,
            RecordedUtc = DateTime.UtcNow
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);

        return plan;
    }

    // -------------------------------- Update ---------------------------------

    public async Task<bool> UpdateIfOpenAsync(Plan incoming, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        if (incoming.Id == Guid.Empty) throw new ArgumentException("plan.Id is required.", nameof(incoming));

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var plan = await _db.Plans
            .Include(p => p.Participants)
            .FirstOrDefaultAsync(p => p.Id == incoming.Id, ct);

        if (plan is null) return false;

        if (plan.State != PlanState.Open)
            throw new InvalidOperationException("План закритий — редагування заборонено.");

        var hasOrder = await _db.Orders.AnyAsync(o => o.PlanId == plan.Id, ct);
        if (hasOrder)
            throw new InvalidOperationException("План має наказ — редагування заборонено.");

        // Поля плану
        if (string.IsNullOrWhiteSpace(incoming.PlanNumber))
            throw new InvalidOperationException("PlanNumber обовʼязковий.");
        plan.PlanNumber = incoming.PlanNumber.Trim();

        plan.Type = incoming.Type;
        plan.PlannedAtUtc = NormalizeToUtc(incoming.PlannedAtUtc);
        plan.TimeKind = incoming.TimeKind;
        plan.Location = string.IsNullOrWhiteSpace(incoming.Location) ? null : incoming.Location.Trim();
        plan.GroupName = string.IsNullOrWhiteSpace(incoming.GroupName) ? null : incoming.GroupName.Trim();
        plan.ToolType = string.IsNullOrWhiteSpace(incoming.ToolType) ? null : incoming.ToolType.Trim();
        plan.TaskDescription = string.IsNullOrWhiteSpace(incoming.TaskDescription) ? null : incoming.TaskDescription.Trim();

        // Оновлення складу (якщо передали Participants у incoming)
        if (incoming.Participants is { Count: > 0 })
        {
            var newIds = incoming.Participants
                .Select(x => x.PersonId)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToHashSet();

            if (newIds.Count == 0)
                throw new InvalidOperationException("План має містити принаймні одного учасника.");

            var currentIds = plan.Participants.Select(s => s.PersonId).ToHashSet();

            var toRemove = currentIds.Except(newIds).ToArray();
            var toAdd = newIds.Except(currentIds).ToArray();

            if (toRemove.Length > 0)
            {
                var rm = plan.Participants.Where(s => toRemove.Contains(s.PersonId)).ToList();
                _db.RemoveRange(rm);
            }

            if (toAdd.Length > 0)
            {
                var personsToAdd = await _db.Persons
                    .Include(p => p.PositionUnit)
                    .Include(p => p.StatusKind)
                    .Where(p => toAdd.Contains(p.Id))
                    .ToListAsync(ct);

                if (personsToAdd.Count != toAdd.Length)
                    throw new InvalidOperationException("Деякі додані учасники не знайдені.");

                foreach (var p in personsToAdd)
                    plan.Participants.Add(BuildSnapshot(plan.Id, p, author: incoming.Author));
            }
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    // -------------------------------- Delete ---------------------------------

    public async Task<bool> DeleteIfOpenAsync(Guid planId, CancellationToken ct = default)
    {
        if (planId == Guid.Empty) throw new ArgumentException("planId is required.", nameof(planId));

        var plan = await _db.Plans
            .Include(p => p.Participants)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return false;

        var hasOrder = await _db.Orders.AnyAsync(o => o.PlanId == planId, ct);
        if (plan.State != PlanState.Open || hasOrder)
            throw new InvalidOperationException("План неможливо видалити: він закритий або має наказ.");

        _db.RemoveRange(plan.Participants);
        _db.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // -------------------------------- Helpers --------------------------------

    private static DateTime NormalizeToUtc(DateTime dt)
        => dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };

    private static PlanParticipantSnapshot BuildSnapshot(Guid planId, Person p, string? author)
        => new()
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            PersonId = p.Id,
            FullName = p.FullName,
            Rank = string.IsNullOrWhiteSpace(p.Rank) ? null : p.Rank,
            PositionSnapshot = p.PositionUnit?.FullName ?? p.PositionUnit?.ShortName,
            Weapon = string.IsNullOrWhiteSpace(p.Weapon) ? null : p.Weapon,
            Callsign = string.IsNullOrWhiteSpace(p.Callsign) ? null : p.Callsign,
            StatusKindId = p.StatusKindId,
            StatusKindCode = p.StatusKind?.Code,
            Author = author,
            RecordedUtc = DateTime.UtcNow
        };
}
*/