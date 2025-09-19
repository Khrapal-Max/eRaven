//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanActionService (lightweight guards)
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanActionService;

public sealed class PlanActionService(AppDbContext db) : IPlanActionService
{
    private readonly AppDbContext _db = db;

    // ---------- читання ----------
    public async Task<IReadOnlyList<PlanAction>> GetByPersonAsync(Guid personId, bool onlyDraft = false, CancellationToken ct = default)
    {
        var q = _db.PlanActions.AsNoTracking().Where(a => a.PersonId == personId);
        if (onlyDraft) q = q.Where(a => a.ActionState == ActionState.PlanAction);
        var list = await q.OrderBy(a => a.EffectiveAtUtc).ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<PlanAction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.PlanActions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    // ---------- створення ----------
    public async Task<PlanAction> AddActionAsync(CreatePlanActionDto dto, CancellationToken ct = default)
    {
        // мінімальні обов'язкові перевірки
        if (dto.PersonId == Guid.Empty) throw new ArgumentException("PersonId is required.");
        if (dto.ToStatusKindId <= 0) throw new ArgumentException("ToStatusKindId is required.");

        var person = await _db.Persons
            .Include(p => p.PositionUnit)
            .Include(p => p.StatusKind)
            .FirstOrDefaultAsync(p => p.Id == dto.PersonId, ct)
            ?? throw new InvalidOperationException("Особа не знайдена.");

        var statusExists = await _db.StatusKinds.AnyAsync(s => s.Id == dto.ToStatusKindId, ct);
        if (!statusExists) throw new InvalidOperationException("Цільовий статус не існує.");

        var effectiveUtc = dto.EffectiveAtUtc.Kind == DateTimeKind.Utc
            ? dto.EffectiveAtUtc
            : DateTime.SpecifyKind(dto.EffectiveAtUtc, DateTimeKind.Utc);

        // ---- важливі гварди ----
        // 1) Dispatch → Return лінійність (простий варіант)
        if (dto.MoveType == MoveType.Return)
        {
            if (dto.TripId is null)
                throw new InvalidOperationException("Для Return потрібно вказати TripId (пара з Dispatch).");

            var dispatch = await _db.PlanActions
                .Where(a => a.PersonId == dto.PersonId
                         && a.MoveType == MoveType.Dispatch
                         && a.TripId == dto.TripId)
                .OrderBy(a => a.EffectiveAtUtc)
                .LastOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Return без попереднього Dispatch (за TripId) заборонено.");

            if (effectiveUtc < dispatch.EffectiveAtUtc)
                throw new InvalidOperationException("Час Return має бути не раніше Dispatch.");
        }
        else // MoveType.Dispatch
        {
            // Не дозволяємо новий Dispatch, якщо є відкритий (без Return) Dispatch
            var openDispatchExists = await _db.PlanActions
                .Where(a => a.PersonId == dto.PersonId && a.MoveType == MoveType.Dispatch)
                .GroupJoin(
                    _db.PlanActions.Where(a => a.PersonId == dto.PersonId && a.MoveType == MoveType.Return),
                    d => d.TripId,
                    r => r.TripId,
                    (d, rs) => new { d, hasReturn = rs.Any() })
                .AnyAsync(x => !x.hasReturn, ct);

            if (openDispatchExists)
                throw new InvalidOperationException("Існує незакритий Dispatch (без Return). Спочатку повернути, потім нове відрядження.");
        }

        // Авто-генерація TripId для Dispatch за потреби
        var tripId = dto.TripId ?? (dto.MoveType == MoveType.Dispatch ? Guid.NewGuid() : null);

        // снапшот
        var positionName = person.PositionUnit?.FullName ?? string.Empty;
        var statusKindOnDate = person.StatusKind?.Code ?? person.StatusKind?.Name ?? string.Empty;

        var action = new PlanAction
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            Person = person,
            EffectiveAtUtc = effectiveUtc,
            ToStatusKindId = dto.ToStatusKindId,
            Order = null,
            ActionState = ActionState.PlanAction,
            MoveType = dto.MoveType,
            TripId = tripId,
            Location = dto.Location,
            GroupName = dto.GroupName,
            CrewName = dto.CrewName,
            Note = dto.Note ?? string.Empty,

            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = positionName,
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? string.Empty,
            Callsign = person.Callsign ?? string.Empty,
            StatusKindOnDate = statusKindOnDate
        };

        _db.PlanActions.Add(action);
        await _db.SaveChangesAsync(ct);
        return action;
    }

    // ---------- видалення ----------
    public async Task<bool> DeleteAsync(Guid actionId, CancellationToken ct = default)
    {
        var action = await _db.PlanActions.FirstOrDefaultAsync(a => a.Id == actionId, ct);
        if (action is null) return false;

        if (action.ActionState != ActionState.PlanAction)
            throw new InvalidOperationException("Неможливо видалити дію, яка вже затверджена наказом.");

        if (action.MoveType == MoveType.Dispatch && action.TripId is not null)
        {
            var hasReturn = await _db.PlanActions.AnyAsync(a =>
                a.PersonId == action.PersonId &&
                a.MoveType == MoveType.Return &&
                a.TripId == action.TripId, ct);

            if (hasReturn)
                throw new InvalidOperationException("Спочатку видаліть парний Return, потім Dispatch.");
        }

        _db.PlanActions.Remove(action);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------- затвердження (одна дія) ----------
    public async Task<ApproveResult> ApproveAsync(Guid actionId, ApproveOptions options, CancellationToken ct = default)
    {
        var action = await _db.PlanActions
            .Include(a => a.Person)
            .FirstOrDefaultAsync(a => a.Id == actionId, ct);

        if (action is null)
            return new ApproveResult(actionId, false, ["Дію не знайдено."]);
        if (action.ActionState != ActionState.PlanAction)
            return new ApproveResult(actionId, false, ["Дія вже не є чернеткою."]);

        // Мінімальні гварди для Return
        var errors = new List<string>();
        if (action.MoveType == MoveType.Return && action.TripId is not null)
        {
            var dispatch = await _db.PlanActions
                .Where(a => a.PersonId == action.PersonId && a.TripId == action.TripId && a.MoveType == MoveType.Dispatch)
                .OrderBy(a => a.EffectiveAtUtc)
                .LastOrDefaultAsync(ct);

            if (dispatch is null) errors.Add("Return без Dispatch (TripId) заборонено.");
            else if (action.EffectiveAtUtc < dispatch.EffectiveAtUtc) errors.Add("Час Return раніше Dispatch.");
        }
        if (errors.Count > 0)
            return new ApproveResult(actionId, false, errors);

        // --- застосування ---
        action.Order = options.OrderName?.Trim();
        action.ActionState = ActionState.ApprovedOrder;

        var person = action.Person;
        var openUtc = action.EffectiveAtUtc.Kind == DateTimeKind.Utc
            ? action.EffectiveAtUtc
            : DateTime.SpecifyKind(action.EffectiveAtUtc, DateTimeKind.Utc);

        // знайти наступний Sequence для (person, openUtc) серед активних
        short nextSeq = (await _db.PersonStatuses
            .Where(s => s.PersonId == person.Id && s.IsActive && s.OpenDate == openUtc)
            .MaxAsync(s => (short?)s.Sequence, ct)) ?? -1;
        nextSeq++;

        var ps = new PersonStatus
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            StatusKindId = action.ToStatusKindId,
            OpenDate = openUtc,
            IsActive = true,
            Sequence = nextSeq,
            Author = string.IsNullOrWhiteSpace(options.Author) ? "system" : options.Author!.Trim(),
            Note = string.IsNullOrWhiteSpace(action.Order) ? "Наказ" : $"Наказ: {action.Order}",
            SourceDocumentType = "PlanAction",
            SourceDocumentId = action.Id,
            Modified = DateTime.UtcNow
        };

        _db.PersonStatuses.Add(ps);

        // оновити «поточний» статус людини
        person.StatusKindId = action.ToStatusKindId;
        person.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return new ApproveResult(actionId, true, []);
    }

    // ---------- пакетне затвердження ----------
    public async Task<BatchApproveResult> ApproveBatchAsync(IEnumerable<Guid> actionIds, ApproveOptions options, CancellationToken ct = default)
    {
        var ids = actionIds.Distinct().ToArray();
        if (ids.Length == 0) return new BatchApproveResult(0, 0, []);

        var results = new List<ApproveResult>();
        int applied = 0;

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        foreach (var id in ids)
        {
            var r = await ApproveAsync(id, options, ct);
            results.Add(r);
            if (r.Applied) applied++;
        }
        await tx.CommitAsync(ct);

        return new BatchApproveResult(ids.Length, applied, results);
    }

    // ---------- «сухі» перевірки ----------
    public async Task<IReadOnlyList<string>> ValidateNewActionAsync(CreatePlanActionDto dto, CancellationToken ct = default)
    {
        var msgs = new List<string>();
        try { _ = await AddActionAsync(dto, ct); } // викликаємо ту ж логіку…
        catch (Exception ex) { msgs.Add(ex.Message); }
        return msgs;
    }

    public async Task<IReadOnlyList<string>> ValidateApproveAsync(IEnumerable<Guid> actionIds, CancellationToken ct = default)
    {
        var msgs = new List<string>();
        var ids = actionIds.Distinct().ToArray();
        var actions = await _db.PlanActions.Where(a => ids.Contains(a.Id)).ToListAsync(ct);

        foreach (var a in actions)
        {
            if (a.ActionState != ActionState.PlanAction)
                msgs.Add($"Дія {a.Id} не чернетка.");
            if (a.MoveType == MoveType.Return && a.TripId is null)
                msgs.Add($"Дія {a.Id}: Return без TripId.");
        }
        return msgs;
    }
}
