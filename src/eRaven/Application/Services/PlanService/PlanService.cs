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

    public async Task<IReadOnlyList<Plan>> GetAllPlansAsync(CancellationToken ct = default)
       => await _db.Plans.AsNoTracking()
           .OrderByDescending(p => p.RecordedUtc)
           .ToListAsync(ct);

    public async Task<Plan?> GetPlanAsync(Guid planId, CancellationToken ct = default)
        => await _db.Plans.AsNoTracking()
            .Include(p => p.Participants)
            .ThenInclude(pp => pp.Actions)
            .SingleOrDefaultAsync(p => p.Id == planId, ct);

    public async Task<IReadOnlyList<PlanParticipant>> GetPlanParticipantsAsync(Guid planId, CancellationToken ct = default)
    {
        var list = await _db.PlanParticipants
            .AsNoTracking()
            .Where(x => x.PlanId == planId)
            .OrderBy(x => x.FullName)
            .ToListAsync(ct);

        return list.AsReadOnly();
    }

    public async Task<IReadOnlyList<PlanParticipantAction>> GetPlanActionsAsync(Guid planId, CancellationToken ct = default)
    {
        var list = await _db.PlanParticipantActions
            .AsNoTracking()
            .Where(a => a.PlanId == planId)
            .OrderBy(a => a.EventAtUtc)
            .ThenBy(a => a.RecordedUtc)
            .ToListAsync(ct);

        return list.AsReadOnly();
    }

    //--------------------------------------------------------------------------
    // EnsurePlanAsync
    //--------------------------------------------------------------------------
    public async Task<Plan> EnsurePlanAsync(CreatePlanViewModel vm, string author, CancellationToken ct = default)
    {
        var planNumber = vm.PlanNumber.Trim();

        var plan = await _db.Plans.SingleOrDefaultAsync(p => p.PlanNumber == planNumber, ct);
        if (plan is not null) return plan;

        plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = planNumber,
            State = PlanState.Open,  // етап 1: завжди Open
            Author = author,
            RecordedUtc = DateTime.UtcNow
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return plan;
    }

    //--------------------------------------------------------------------------
    // EnsureParticipantAsync
    //--------------------------------------------------------------------------
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

        // Снапшот атрибутів (валідність полів гарантують довідники + БД-констрейнти)
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

    //--------------------------------------------------------------------------
    // AddActionAndApplyStatusAsync
    //--------------------------------------------------------------------------
    public async Task<PlanParticipantAction> AddActionAndApplyStatusAsync(PlanActionViewModel vm, string author, CancellationToken ct = default)
    {
        var planNumber = vm.PlanNumber.Trim();
        var eventAtUtc = EnsureUtc(vm.EventAtUtc);

        // НІЯКИХ BeginTransaction тут

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
        } ?? throw new InvalidOperationException("PlanServiceOptions not configured: target StatusKind is required.");

        await InsertPersonStatusAsync(
            personId: pp.PersonId,
            statusKindId: statusKindId,
            openDateUtc: eventAtUtc,
            note: BuildStatusNote(planNumber, vm.ActionType, vm.Location, vm.GroupName, vm.CrewName),
            author: author,
            ct: ct);

        var person = await _db.Persons.SingleAsync(p => p.Id == pp.PersonId, ct);
        person.StatusKindId = statusKindId;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct); // EF сам зробить транзакцію для цього набору змін
        return action;
    }

    //--------------------------------------------------------------------------
    // ApplyBatchAsync
    //--------------------------------------------------------------------------
    public async Task ApplyBatchAsync(PlanBatchViewModel vm, string author, CancellationToken ct = default)
    {
        var planNumber = vm.PlanNumber.Trim();

        using var tx = await _db.Database.BeginTransactionAsync(ct);

        await EnsurePlanAsync(new CreatePlanViewModel { PlanNumber = planNumber }, author, ct);

        foreach (var group in vm.Actions.GroupBy(a => a.PersonId))
        {
            await EnsureParticipantAsync(planNumber, group.Key, author, ct);

            foreach (var a in group.OrderBy(x => x.EventAtUtc))
            {
                var singleVm = new PlanActionViewModel
                {
                    PlanNumber = planNumber,
                    PersonId = group.Key,
                    ActionType = a.ActionType,
                    EventAtUtc = EnsureUtc(a.EventAtUtc),
                    Location = a.Location,
                    GroupName = a.GroupName,
                    CrewName = a.CrewName,
                    Note = a.Note
                };

                await AddActionAndApplyStatusAsync(singleVm, author, ct);
            }
        }

        await tx.CommitAsync(ct);
    }

    public async Task<bool> DeletePlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;

        if (plan.State != PlanState.Open)
            throw new InvalidOperationException("План закритий — видалення заборонено.");

        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    //--------------------------------------------------------------------------
    // Допоміжні
    //--------------------------------------------------------------------------

    private static DateTime EnsureUtc(DateTime dt)
        => dt.Kind switch
        {
            DateTimeKind.Utc => dt,
            DateTimeKind.Local => dt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
        };

    private static string BuildStatusNote(string planNumber, PlanActionType type, string? loc, string? grp, string? crew)
        => $"Plan {planNumber}: {type} [{loc}/{grp}/{crew}]";

    /// <summary>
    /// Забороняє "Return без Dispatch", "подвійний Dispatch", та повернення раніше Dispatch.
    /// (мінімальні гварди бізнес-логіки)
    /// </summary>
    private async Task ValidateActionAsync(Guid planParticipantId, PlanActionType newType, DateTime newEventAtUtc, CancellationToken ct)
    {
        var actions = await _db.PlanParticipantActions
            .Where(x => x.PlanParticipantId == planParticipantId)
            .OrderBy(x => x.EventAtUtc)
            .ThenBy(x => x.RecordedUtc)
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

    /// <summary>
    /// Додає запис у журнал PersonStatuses (без закриття попередніх).
    /// UTC-нормалізація, idempotency, коректний short Sequence і (опційно) перевірка StatusTransition.
    /// </summary>
    private async Task InsertPersonStatusAsync(
        Guid personId,
        int statusKindId,
        DateTime openDateUtc,
        string? note,
        string author,
        CancellationToken ct)
    {
        // UTC
        openDateUtc = openDateUtc.Kind switch
        {
            DateTimeKind.Utc => openDateUtc,
            DateTimeKind.Local => openDateUtc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(openDateUtc, DateTimeKind.Utc)
        };

        // Idempotency
        var exists = await _db.PersonStatuses.AsNoTracking().AnyAsync(
            s => s.PersonId == personId
              && s.IsActive
              && s.OpenDate == openDateUtc
              && s.StatusKindId == statusKindId,
            ct);
        if (exists) return;

        // (Опційно) Перевірка StatusTransition
        var currentKindId = await _db.Persons
            .Where(p => p.Id == personId)
            .Select(p => p.StatusKindId)
            .SingleAsync(ct);

        if (currentKindId is not null)
        {
            var allowed = await _db.StatusTransitions.AnyAsync(t =>
                t.FromStatusKindId == currentKindId.Value &&
                t.ToStatusKindId == statusKindId, ct);

            if (!allowed)
                throw new InvalidOperationException($"Перехід статусу заборонено: {currentKindId} → {statusKindId}.");
        }

        // short Sequence на той самий момент
        var maxSeq = await _db.PersonStatuses
          .Where(s => s.PersonId == personId && s.IsActive && s.OpenDate == openDateUtc)
          .Select(s => (short?)s.Sequence)
          .MaxAsync(ct); // => null якщо записів немає

        short nextSeq = (short)(maxSeq.GetValueOrDefault(-1) + 1);

        var ps = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            StatusKindId = statusKindId,
            Sequence = nextSeq,
            OpenDate = openDateUtc,
            Note = note,
            IsActive = true,
            Author = author,
            Modified = DateTime.UtcNow
        };

        _db.PersonStatuses.Add(ps);
    }
}
