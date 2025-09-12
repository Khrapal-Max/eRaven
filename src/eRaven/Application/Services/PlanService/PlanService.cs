//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanService (мінімальна логіка: приймає готовий план від UI)
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

    // ------------------------------- Queries --------------------------------

    // Регістр: лише заголовки
    public async Task<IEnumerable<Plan>> GetAllPlansAsync(CancellationToken ct = default)
        => await _db.Plans.AsNoTracking()
            .OrderByDescending(p => p.RecordedUtc)
            .ToListAsync(ct);

    // Повний план для перегляду/редагування
    public async Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct = default)
        => await _db.Plans.AsNoTracking()
            .Include(p => p.PlanElements)
                .ThenInclude(pe => pe.Participants)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

    // ------------------------------- Create ---------------------------------

    /// <summary>
    /// Створити план. Мінімальна валідація: номер, наявність елементів, у кожному елементі ≥1 учасник
    /// з обовʼязковими полями (FullName, Rnokpp, PersonId). Час подій нормалізуємо до UTC.
    /// Жодних «правил статусів» тут немає — все валідовано на сторінці.
    /// </summary>
    public async Task<Plan> CreateAsync(CreatePlanViewModel vm, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vm);
        if (string.IsNullOrWhiteSpace(vm.PlanNumber))
            throw new ArgumentException("PlanNumber обовʼязковий.", nameof(vm));

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = vm.PlanNumber.Trim(),
            State = vm.State, // зазвичай Open
            Author = null,
            RecordedUtc = DateTime.UtcNow,
            PlanElements = [.. vm.PlanElements.Select(NormalizeElementForCreate)]
        };

        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return plan;
    }

    // ------------------------------- Delete ---------------------------------

    public async Task<bool> DeleteIfOpenAsync(Guid planId, CancellationToken ct = default)
    {
        if (planId == Guid.Empty) throw new ArgumentException("planId is required.", nameof(planId));

        var plan = await _db.Plans
            .Include(p => p.PlanElements)
                .ThenInclude(pe => pe.Participants)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return false;

        var hasOrder = await _db.Orders.AnyAsync(o => o.PlanId == planId, ct);
        if (plan.State != PlanState.Open || hasOrder)
            throw new InvalidOperationException("План неможливо видалити: він закритий або має наказ.");

        _db.Remove(plan); // каскадом підуть елементи та їхні учасники
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ----------------------- Roster для модала ------------------------------

    public async Task<PlanRosterResponse> GetPlanRosterAsync(Guid planId, CancellationToken ct = default)
    {
        // 1) Витягуємо всіх людей з їх статусом і посадою
        var persons = await _db.Persons
            .AsNoTracking()
            .Include(p => p.StatusKind)
            .Include(p => p.PositionUnit)
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ThenBy(p => p.MiddleName)
            .ToListAsync(ct);

        // 2) Поточний граф плану
        var plan = await _db.Plans
            .AsNoTracking()
            .Include(p => p.PlanElements)
                .ThenInclude(e => e.Participants)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        var byPerson = new Dictionary<Guid, (PlanType? LastAction, PlanElement? LastDispatch)>();

        if (plan is not null)
        {
            var items = plan.PlanElements
                .OrderBy(e => e.EventAtUtc)
                .ToList();

            foreach (var e in items)
            {
                foreach (var p in e.Participants)
                {
                    if (!byPerson.TryGetValue(p.PersonId, out var x)) x = (null, null);
                    x.LastAction = e.Type;
                    if (e.Type == PlanType.Dispatch) x.LastDispatch = e;
                    byPerson[p.PersonId] = x;
                }
            }
        }

        var list = new List<PersonPlanInfo>(persons.Count);
        foreach (var p in persons)
        {
            byPerson.TryGetValue(p.Id, out var st);
            list.Add(new PersonPlanInfo(
                PersonId: p.Id,
                FullName: p.FullName,
                Rnokpp: p.Rnokpp ?? string.Empty,
                Rank: string.IsNullOrWhiteSpace(p.Rank) ? null : p.Rank,
                Position: p.PositionUnit?.FullName ?? p.PositionUnit?.ShortName,
                Weapon: string.IsNullOrWhiteSpace(p.Weapon) ? null : p.Weapon,
                Callsign: string.IsNullOrWhiteSpace(p.Callsign) ? null : p.Callsign,
                StatusKindId: p.StatusKindId,
                StatusKindCode: p.StatusKind?.Code,
                StatusKindName: p.StatusKind?.Name,
                LastPlannedAction: st.LastAction,
                LastDispatchLocation: st.LastDispatch?.Location,
                LastDispatchGroup: st.LastDispatch?.GroupName,
                LastDispatchTool: st.LastDispatch?.ToolType
            ));
        }

        return new PlanRosterResponse(list);
    }

    // -------------------- Add: один елемент, багато людей -------------------

    public async Task<AddElementsResult> AddElementsAsync(AddElementsRequest req, CancellationToken ct = default)
    {
        if (req is null) throw new ArgumentNullException(nameof(req));
        if (req.PlanId == Guid.Empty) throw new ArgumentException("PlanId required.", nameof(req));
        if (req.PersonIds is null || req.PersonIds.Count == 0)
            throw new InvalidOperationException("Вкажіть хоча б одну особу.");
        if (req.EventAtUtc == default) throw new InvalidOperationException("EventAtUtc обов’язковий.");

        var plan = await _db.Plans
            .Include(p => p.PlanElements)
                .ThenInclude(e => e.Participants)
            .FirstOrDefaultAsync(p => p.Id == req.PlanId, ct);

        if (plan is null) throw new InvalidOperationException("План не знайдено.");
        if (plan.State != PlanState.Open) throw new InvalidOperationException("План закритий — редагування заборонено.");

        var evUtc = NormalizeQuarterUtc(req.EventAtUtc);

        // антидубль/чередування + базова перевірка статусу
        var persons = await _db.Persons
            .Include(p => p.StatusKind)
            .Where(p => req.PersonIds.Contains(p.Id))
            .ToListAsync(ct);

        if (persons.Count != req.PersonIds.Count)
            throw new InvalidOperationException("Деяких осіб не знайдено.");

        // стан у плані (остання дія + останній Dispatch)
        var lastByPerson = plan.PlanElements
            .OrderBy(e => e.EventAtUtc)
            .SelectMany(e => e.Participants.Select(pp => new { pp.PersonId, e.Type, e }))
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g =>
            {
                var arr = g.OrderBy(y => y.e.EventAtUtc).ToArray();
                var last = arr.Last();
                var lastDisp = arr.Where(z => z.Type == PlanType.Dispatch).Select(z => z.e).LastOrDefault();
                return (Last: last.Type, LastDispatch: lastDisp);
            });

        // Для Return — контекст має бути узгодженим у межах елемента (один для всіх)
        if (req.Type == PlanType.Return)
        {
            foreach (var pid in req.PersonIds)
            {
                if (!lastByPerson.TryGetValue(pid, out var L) || L.Last != PlanType.Dispatch)
                    throw new InvalidOperationException("Повернення можливе лише після відрядження в межах плану.");
                // якщо задані Location/Group/Tool — звіряємо з останнім Dispatch
                if (!ContextMatches(req, L.LastDispatch))
                    throw new InvalidOperationException("Контекст повернення не відповідає відрядженню. Додайте цих людей окремим елементом.");
            }
        }

        // Для Dispatch — особи зі статусом != AREA не дозволені
        if (req.Type == PlanType.Dispatch)
        {
            var bad = persons.FirstOrDefault(p => !string.Equals(p.StatusKind?.Code, "AREA", StringComparison.OrdinalIgnoreCase));
            if (bad is not null)
                throw new InvalidOperationException($"Особа {bad.FullName} має статус не 'В районі'. Відрядження недоступне.");
        }

        // Антидубль у плані: same person+type+time
        foreach (var pid in req.PersonIds)
        {
            var exists = plan.PlanElements.Any(e =>
                e.EventAtUtc == evUtc &&
                e.Type == req.Type &&
                e.Participants.Any(pp => pp.PersonId == pid));
            if (exists)
                throw new InvalidOperationException("У плані вже є така дія на цей час для однієї з осіб.");
        }

        // Побудова елемента
        var el = new PlanElement
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            Type = req.Type,
            EventAtUtc = evUtc,
            Location = T(req.Location),
            GroupName = T(req.GroupName),
            ToolType = T(req.ToolType),
            Note = T(req.Note),
            Author = "ui",
            RecordedUtc = DateTime.UtcNow,
            Participants = []
        };

        // Якщо Return без явного контексту — підставимо з першого Dispatch (вони вже звірені)
        if (req.Type == PlanType.Return && el.Location is null && el.GroupName is null && el.ToolType is null)
        {
            var firstPid = req.PersonIds.First();
            if (lastByPerson.TryGetValue(firstPid, out var LL) && LL.LastDispatch is not null)
            {
                el.Location = T(LL.LastDispatch.Location);
                el.GroupName = T(LL.LastDispatch.GroupName);
                el.ToolType = T(LL.LastDispatch.ToolType);
            }
        }

        foreach (var p in persons)
        {
            el.Participants.Add(new PlanParticipantSnapshot
            {
                Id = Guid.NewGuid(),
                PlanElementId = el.Id,
                PersonId = p.Id,
                FullName = p.FullName,
                Rnokpp = p.Rnokpp ?? string.Empty,
                Rank = T(p.Rank),
                PositionSnapshot = p.PositionUnit?.FullName ?? p.PositionUnit?.ShortName,
                Weapon = T(p.Weapon),
                Callsign = T(p.Callsign),
                StatusKindId = p.StatusKindId,
                StatusKindCode = T(p.StatusKind?.Code),
                Author = "ui",
                RecordedUtc = DateTime.UtcNow
            });
        }

        _db.PlanElements.Add(el);
        await _db.SaveChangesAsync(ct);

        return new AddElementsResult(el.Id, el.Participants.Count);
    }

    // ----------------------------- Remove (учасник) -------------------------

    public async Task<RemoveParticipantResult> RemoveParticipantAsync(RemoveParticipantRequest request, CancellationToken ct = default)
    {
        var plan = await _db.Plans
            .Include(p => p.PlanElements)
                .ThenInclude(e => e.Participants)
            .FirstOrDefaultAsync(p => p.Id == request.PlanId, ct);

        if (plan is null) return new(false, false);
        if (plan.State != PlanState.Open) throw new InvalidOperationException("План закритий — редагування заборонено.");

        var el = plan.PlanElements.FirstOrDefault(e => e.Id == request.ElementId);
        if (el is null) return new(false, false);

        var snap = el.Participants.FirstOrDefault(x => x.PersonId == request.PersonId);
        if (snap is null) return new(false, false);

        el.Participants.Remove(snap);
        _db.Remove(snap);

        var elementDeleted = false;
        if (el.Participants.Count == 0)
        {
            _db.Remove(el);
            elementDeleted = true;
        }

        await _db.SaveChangesAsync(ct);
        return new(true, elementDeleted);
    }

    // ------------------------------- Helpers --------------------------------

    private static bool ContextMatches(AddElementsRequest req, PlanElement? dispatch)
    {
        if (dispatch is null) return false;
        string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        return T(req.Location) == T(dispatch.Location)
            && T(req.GroupName) == T(dispatch.GroupName)
            && T(req.ToolType) == T(dispatch.ToolType);
    }

    private static DateTime NormalizeQuarterUtc(DateTime dtUtc)
    {
        var utc = dtUtc.Kind == DateTimeKind.Utc ? dtUtc
            : (dtUtc.Kind == DateTimeKind.Local ? dtUtc.ToUniversalTime() : DateTime.SpecifyKind(dtUtc, DateTimeKind.Utc));

        var m = utc.Minute;
        var mm = m < 15 ? 0 : m < 30 ? 15 : m < 45 ? 30 : 45;
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, mm, 0, DateTimeKind.Utc);
    }

    private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    // ------------------------------- Helpers --------------------------------

    private static string? Normalize(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static DateTime ToUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    };

    private static PlanElement NormalizeElementForCreate(PlanElement e)
        => NormalizeElementCore(e, planId: Guid.Empty);

    private static PlanElement NormalizeElementForUpdate(PlanElement e, Guid planId)
        => NormalizeElementCore(e, planId);

    private static PlanElement NormalizeElementCore(PlanElement e, Guid planId)
    {
        if (e is null) throw new InvalidOperationException("Елемент плану не заданий.");
        if (e.EventAtUtc == default) throw new InvalidOperationException("EventAtUtc обовʼязковий.");
        if (e.Participants is null || e.Participants.Count == 0)
            throw new InvalidOperationException("Елемент має містити принаймні одного учасника.");

        var elementId = e.Id == Guid.Empty ? Guid.NewGuid() : e.Id;

        return new PlanElement
        {
            Id = elementId,
            PlanId = planId == Guid.Empty ? e.PlanId : planId,
            Type = e.Type,
            EventAtUtc = ToUtc(e.EventAtUtc),
            Location = Normalize(e.Location),
            GroupName = Normalize(e.GroupName),
            ToolType = Normalize(e.ToolType),
            Note = Normalize(e.Note),
            Author = Normalize(e.Author),
            RecordedUtc = e.RecordedUtc == default ? DateTime.UtcNow : e.RecordedUtc,

            Participants = [.. e.Participants.Select(p => NormalizeSnapshot(p, elementId, e.Author))]
        };
    }

    private static PlanParticipantSnapshot NormalizeSnapshot(PlanParticipantSnapshot s, Guid elementId, string? fallbackAuthor)
    {
        if (s.PersonId == Guid.Empty)
            throw new InvalidOperationException("Учасник без PersonId.");
        if (string.IsNullOrWhiteSpace(s.FullName))
            throw new InvalidOperationException("Учасник без FullName.");
        if (string.IsNullOrWhiteSpace(s.Rnokpp))
            throw new InvalidOperationException("Учасник без РНОКПП.");

        return new PlanParticipantSnapshot
        {
            Id = s.Id == Guid.Empty ? Guid.NewGuid() : s.Id,
            PlanElementId = elementId,
            PersonId = s.PersonId,
            FullName = s.FullName.Trim(),
            Rnokpp = s.Rnokpp.Trim(),
            Rank = Normalize(s.Rank),
            PositionSnapshot = Normalize(s.PositionSnapshot),
            Weapon = Normalize(s.Weapon),
            Callsign = Normalize(s.Callsign),
            StatusKindId = s.StatusKindId,
            StatusKindCode = Normalize(s.StatusKindCode),
            Author = Normalize(s.Author ?? fallbackAuthor),
            RecordedUtc = s.RecordedUtc == default ? DateTime.UtcNow : s.RecordedUtc
        };
    }
}
