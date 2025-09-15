//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// Application: Services: Implementations
// План: етап 1 — записуємо планові дії і ОДРАЗУ виставляємо фактичні статуси
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

    // ---------- Read ----------
    public async Task<IReadOnlyList<Plan>> GetAllPlansAsync(CancellationToken ct = default)
        => (await _db.Plans.AsNoTracking()
               .OrderByDescending(p => p.RecordedUtc)
               .ToListAsync(ct))
           .AsReadOnly();

    public async Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct = default)
        => await _db.Plans.AsNoTracking()
               .SingleOrDefaultAsync(p => p.Id == planId, ct);

    public async Task<IReadOnlyList<PlanParticipant>> GetPlanParticipantsAsync(Guid planId, CancellationToken ct = default)
        => (await _db.PlanParticipants.AsNoTracking()
               .Where(x => x.PlanId == planId)
               .OrderBy(x => x.FullName)
               .ToListAsync(ct))
           .AsReadOnly();

    public async Task<IReadOnlyList<PlanParticipantAction>> GetPlanActionsAsync(Guid planId, CancellationToken ct = default)
        => (await _db.PlanParticipantActions.AsNoTracking()
               .Where(a => a.PlanId == planId)
               .OrderBy(a => a.EventAtUtc)
               .ThenBy(a => a.RecordedUtc)
               .ToListAsync(ct))
           .AsReadOnly();

    // ---------- Commands (твоя чинна логіка) ----------
    public async Task<Plan> EnsurePlanAsync(CreatePlanViewModel vm, string author, CancellationToken ct = default)
    {
        var planNumber = vm.PlanNumber.Trim();

        var plan = await _db.Plans.SingleOrDefaultAsync(p => p.PlanNumber == planNumber, ct);
        if (plan is not null) return plan;

        plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = planNumber,
            State = PlanState.Open,
            Author = author,
            RecordedUtc = DateTime.UtcNow
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<PlanParticipant> EnsureParticipantAsync(string planNumber, Guid personId, string author, CancellationToken ct = default)
    {
        var plan = await EnsurePlanAsync(new CreatePlanViewModel { PlanNumber = planNumber }, author, ct);

        var existing = await _db.PlanParticipants
            .SingleOrDefaultAsync(x => x.PlanId == plan.Id && x.PersonId == personId, ct);
        if (existing is not null) return existing;

        var person = await _db.Persons
            .Include(p => p.PositionUnit)
            .SingleOrDefaultAsync(p => p.Id == personId, ct)
            ?? throw new InvalidOperationException($"Person not found: {personId}");

        var pp = new PlanParticipant
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = personId,
            FullName = person.FullName.Trim(),
            RankName = (person.Rank ?? string.Empty).Trim(),
            PositionName = (person.PositionUnit?.ShortName ?? string.Empty).Trim(),
            UnitName = (person.PositionUnit?.OrgPath ?? string.Empty).Trim(),
            Author = author,
            RecordedUtc = DateTime.UtcNow
        };

        _db.PlanParticipants.Add(pp);
        await _db.SaveChangesAsync(ct);
        return pp;
    }

    // залишаємо твою реалізацію AddActionAndApplyStatusAsync/ApplyBatchAsync/InsertPersonStatusAsync/ValidateActionAsync
    // (див. попередні ваші повідомлення). Тут – скорочено для фокусу на нових методах.

    public async Task<PlanParticipantAction> AddActionAndApplyStatusAsync(PlanActionViewModel vm, string author, CancellationToken ct = default)
    {
        var planNumber = vm.PlanNumber.Trim();
        var eventAtUtc = vm.EventAtUtc.Kind switch
        {
            DateTimeKind.Utc => vm.EventAtUtc,
            DateTimeKind.Local => vm.EventAtUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(vm.EventAtUtc, DateTimeKind.Utc)
        };

        using var tx = await _db.Database.BeginTransactionAsync(ct);

        var pp = await EnsureParticipantAsync(planNumber, vm.PersonId, author, ct);
        await ValidateActionAsync(pp.Id, vm.ActionType, eventAtUtc, ct);

        var action = new PlanParticipantAction
        {
            Id = Guid.NewGuid(),
            PlanParticipantId = pp.Id,
            PlanId = pp.PlanId,
            PersonId = pp.PersonId,
            ActionType = vm.ActionType,
            EventAtUtc = eventAtUtc,
            Location = vm.Location.Trim(),
            GroupName = vm.GroupName.Trim(),
            CrewName = vm.CrewName.Trim(),
            Note = vm.Note,
            Author = author,
            RecordedUtc = DateTime.UtcNow
        };
        _db.PlanParticipantActions.Add(action);

        var opts = await _db.PlanServiceOptions.SingleAsync(ct);
        var statusKindId = vm.ActionType switch
        {
            PlanActionType.Dispatch => opts.DispatchStatusKindId,
            PlanActionType.Return => opts.ReturnStatusKindId,
            _ => null
        } ?? throw new InvalidOperationException("PlanServiceOptions not configured.");

        await InsertPersonStatusAsync(pp.PersonId, statusKindId, eventAtUtc,
            note: $"Plan {planNumber}: {vm.ActionType} [{vm.Location}/{vm.GroupName}/{vm.CrewName}]",
            author, ct);

        var person = await _db.Persons.SingleAsync(p => p.Id == pp.PersonId, ct);
        person.StatusKindId = statusKindId;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return action;
    }

    public async Task ApplyBatchAsync(PlanBatchViewModel vm, string author, CancellationToken ct = default)
    {
        var planNumber = vm.PlanNumber.Trim();

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        await EnsurePlanAsync(new CreatePlanViewModel { PlanNumber = planNumber }, author, ct);

        foreach (var group in vm.Actions.GroupBy(a => a.PersonId))
        {
            var pp = await EnsureParticipantAsync(planNumber, group.Key, author, ct);

            foreach (var a in group.OrderBy(x => x.EventAtUtc))
            {
                var localVm = new PlanActionViewModel
                {
                    PlanNumber = planNumber,
                    PersonId = group.Key,
                    ActionType = a.ActionType,
                    EventAtUtc = a.EventAtUtc,
                    Location = a.Location,
                    GroupName = a.GroupName,
                    CrewName = a.CrewName,
                    Note = a.Note
                };

                await AddActionAndApplyStatusAsync(localVm, author, ct);
            }
        }

        await tx.CommitAsync(ct);
    }

    // ---------- Lifecycle ----------
    public async Task<bool> ClosePlanAsync(Guid planId, string author, CancellationToken ct = default)
    {
        var plan = await _db.Plans.SingleOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;

        if (plan.State == PlanState.Close) return true;

        plan.State = PlanState.Close;
        plan.Author = author;
        plan.RecordedUtc = plan.RecordedUtc == default ? DateTime.UtcNow : plan.RecordedUtc;

        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeletePlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.SingleOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;

        if (plan.State != PlanState.Open)
            throw new InvalidOperationException("План закритий — видалення заборонено.");

        _db.Plans.Remove(plan); // каскад: учасники + дії
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------- Helpers (з вашої реалізації) ----------
    private async Task ValidateActionAsync(Guid planParticipantId, PlanActionType newType, DateTime newEventAtUtc, CancellationToken ct)
    {
        var actions = await _db.PlanParticipantActions
            .Where(x => x.PlanParticipantId == planParticipantId)
            .OrderBy(x => x.EventAtUtc).ThenBy(x => x.RecordedUtc)
            .ToListAsync(ct);

        var last = actions.LastOrDefault();

        if (newType == PlanActionType.Dispatch)
        {
            if (last is not null && last.ActionType == PlanActionType.Dispatch)
                throw new InvalidOperationException("Duplicate Dispatch without Return is not allowed.");

            if (last is not null && newEventAtUtc <= last.EventAtUtc)
                throw new InvalidOperationException("Dispatch must be later than previous action time.");
        }
        else // Return
        {
            if (last is null || last.ActionType != PlanActionType.Dispatch)
                throw new InvalidOperationException("Return without prior Dispatch is not allowed.");

            if (newEventAtUtc <= last.EventAtUtc)
                throw new InvalidOperationException("Return cannot be earlier than or equal to the last Dispatch time.");
        }
    }

    private async Task InsertPersonStatusAsync(Guid personId, int statusKindId, DateTime openDateUtc, string? note, string author, CancellationToken ct)
    {
        // UTC нормалізація
        openDateUtc = openDateUtc.Kind switch
        {
            DateTimeKind.Utc => openDateUtc,
            DateTimeKind.Local => openDateUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(openDateUtc, DateTimeKind.Utc)
        };

        // Idempotency
        var exists = await _db.PersonStatuses.AsNoTracking().AnyAsync(
            s => s.PersonId == personId && s.IsActive && s.OpenDate == openDateUtc && s.StatusKindId == statusKindId, ct);
        if (exists) return;

        // Allowed transitions (опційно жорстко — у вас таблиця StatusTransitions заповнена сидом)
        var currentKindId = await _db.Persons.Where(p => p.Id == personId).Select(p => p.StatusKindId).SingleAsync(ct);
        if (currentKindId is not null)
        {
            var allowed = await _db.StatusTransitions.AnyAsync(t =>
                t.FromStatusKindId == currentKindId.Value && t.ToStatusKindId == statusKindId, ct);
            if (!allowed) throw new InvalidOperationException($"Перехід статусу заборонено: {currentKindId} → {statusKindId}.");
        }

        var maxSeq = await _db.PersonStatuses
            .Where(s => s.PersonId == personId && s.IsActive && s.OpenDate == openDateUtc)
            .Select(s => (short?)s.Sequence)
            .MaxAsync(ct);

        short nextSeq = (short)((maxSeq ?? -1) + 1);

        var ps = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            StatusKindId = statusKindId,
            OpenDate = openDateUtc,
            Sequence = nextSeq,
            IsActive = true,
            Note = note,
            Author = author,
            Modified = DateTime.UtcNow
        };

        _db.PersonStatuses.Add(ps);
    }
}