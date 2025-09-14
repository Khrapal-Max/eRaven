// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanService
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanService;

public class PlanService(AppDbContext db) : IPlanService
{
    private readonly AppDbContext _db = db;

    // ---------------- Plans ----------------

    public async Task<IEnumerable<Plan>> GetAllPlansAsync(CancellationToken ct = default)
        => await _db.Plans.AsNoTracking().OrderByDescending(p => p.RecordedUtc).ToListAsync(ct);

    public async Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct = default)
        => await _db.Plans.AsNoTracking()
            .Include(p => p.PlanElements.OrderBy(e => e.EventAtUtc))
                .ThenInclude(e => e.PlanParticipantSnapshot)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

    public async Task<Plan> CreateAsync(CreatePlanViewModel vm, CancellationToken ct = default)
    {
        var number = (vm.PlanNumber ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(number))
            throw new InvalidOperationException("№ плану обов’язковий.");

        var exists = await _db.Plans.AnyAsync(p => p.PlanNumber == number, ct);
        if (exists) throw new InvalidOperationException("План з таким номером вже існує.");

        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            PlanNumber = number,
            State = vm.State,
            Author = "system",
            RecordedUtc = DateTime.UtcNow,
            PlanElements = []
        };
        _db.Plans.Add(plan);
        await _db.SaveChangesAsync(ct);
        return plan;
    }

    public async Task<bool> CloseAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;
        if (plan.State == PlanState.Close) return true;

        plan.State = PlanState.Close;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteIfOpenAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;
        if (plan.State != PlanState.Open)
            throw new InvalidOperationException("План закритий — видалення заборонено.");

        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------------- Elements ----------------

    public async Task<IReadOnlyList<PlanElement>> AddElementsAsync(
        Guid planId,
        IEnumerable<CreatePlanElementViewModel> items,
        CancellationToken ct = default)
    {
        var list = items.ToList();
        if (list.Count == 0) return [];

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // попередньо перевіримо план
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct)
                   ?? throw new InvalidOperationException("План не знайдено.");
        if (plan.State != PlanState.Open)
            throw new InvalidOperationException("План закритий — редагування заборонено.");

        var created = new List<PlanElement>(list.Count);
        var affectedPersons = new HashSet<Guid>();

        foreach (var item in list)
        {
            var el = await AddElementInternalAsync(plan, item, saveImmediately: false, ct);
            created.Add(el);
            affectedPersons.Add(el.PersonId);
        }

        await _db.SaveChangesAsync(ct);

        // Після фактичного зберігання — перебудувати planning по кожній особі
        foreach (var pid in affectedPersons)
            await RebuildPlanningAsync(pid, ct);

        await tx.CommitAsync(ct);
        return created.AsReadOnly();
    }

    public async Task<bool> RemoveElementAsync(Guid planId, Guid elementId, CancellationToken ct = default)
    {
        // 1) План має існувати та бути відкритим
        var state = await _db.Plans
            .Where(p => p.Id == planId)
            .Select(p => p.State)
            .FirstOrDefaultAsync(ct);

        if (state == default) return false;
        if (state != PlanState.Open)
            throw new InvalidOperationException("План закритий — редагування заборонено.");

        // 2) Витягуємо PersonId і момент події елемента, який видаляємо
        var target = await _db.PlanElements
            .Where(e => e.Id == elementId && e.PlanId == planId)
            .Select(e => new { e.PersonId, e.EventAtUtc })
            .FirstOrDefaultAsync(ct);

        if (target is null) return false;

        // 3) Каскад: видалити сам елемент і ВСІ наступні по цій особі в межах цього плану
        //    (=> не залишимо «Відрядити → Відрядити» після видалення Return між ними)
        var affected = await _db.PlanElements
            .Where(e => e.PlanId == planId
                        && e.PersonId == target.PersonId
                        && e.EventAtUtc >= target.EventAtUtc)
            .ExecuteDeleteAsync(ct);

        if (affected == 0) return false;

        // 4) За потреби — перебудувати денормалізовану read-model планування
        //    (залиште виклик, якщо ви використовуєте PersonPlanning; інакше приберіть)
        await RebuildPlanningAsync(target.PersonId, ct);

        return true;
    }

    // ---------------- Internal core ----------------

    private async Task<PlanElement> AddElementInternalAsync(
        Plan plan,
        CreatePlanElementViewModel item,
        bool saveImmediately,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(item);

        // 1) Приведення часу до UTC + контроль кварталу
        var tUtc = item.EventAtUtc.Kind == DateTimeKind.Utc
            ? item.EventAtUtc
            : item.EventAtUtc.ToUniversalTime();
        if (!PlanElement.IsQuarterAligned(tUtc))
            throw new InvalidOperationException("Час має бути на інтервалах 00/15/30/45 без секунд.");

        // 2) Особа + поточний статус
        var person = await _db.Persons
            .Include(x => x.StatusKind)
            .Include(x => x.PositionUnit)
            .FirstOrDefaultAsync(x => x.Id == item.PersonId, ct)
            ?? throw new InvalidOperationException("Особу не знайдено.");

        // 3) Глобальні гварди за PersonPlanning (лінійність/чергування)
        var planning = await _db.PersonPlannings
            .AsNoTracking()
            .FirstOrDefaultAsync(pp => pp.PersonId == person.Id, ct);

        if (planning is not null)
        {
            // дата строго > останнього факту
            if (planning.LastActionAtUtc.HasValue && tUtc <= planning.LastActionAtUtc.Value)
                throw new InvalidOperationException("Дата/час мають бути пізнішими за останню дію цієї особи.");

            // чергування дій
            if (planning.LastActionType.HasValue && planning.LastActionType.Value == item.Type)
                throw new InvalidOperationException("Одна й та сама дія двічі поспіль заборонена.");
        }

        // 4) Предикати дозволеності (з урахуванням статусу)
        if (item.Type == PlanType.Dispatch)
        {
            // статус «В районі»
            if (!IsArea(person))
                throw new InvalidOperationException("Відрядити можна лише зі статусом «В районі».");

            // у межах цього плану — без дубля на той самий момент
            var dup = await _db.PlanElements.AnyAsync(e =>
                e.PlanId == plan.Id &&
                e.PersonId == person.Id &&
                e.Type == PlanType.Dispatch &&
                e.EventAtUtc == tUtc, ct);
            if (dup) throw new InvalidOperationException("На цей момент у цьому плані вже є «Відрядити» для цієї особи.");
        }
        else // Return
        {
            // має бути відкритий виїзд глобально (останнє — Dispatch)
            if (planning is null || planning.LastActionType != PlanType.Dispatch)
                throw new InvalidOperationException("Немає відкритого відрядження — повернення неможливе.");

            // час строго пізніше відкритого Dispatch
            if (planning.LastActionAtUtc.HasValue && tUtc <= planning.LastActionAtUtc.Value)
                throw new InvalidOperationException("Повернення має бути пізніше за відрядження.");

            // анти-дубль у межах плану для Return
            var dup = await _db.PlanElements.AnyAsync(e =>
                e.PlanId == plan.Id &&
                e.PersonId == person.Id &&
                e.Type == PlanType.Return &&
                e.EventAtUtc == tUtc, ct);
            if (dup) throw new InvalidOperationException("На цей момент у цьому плані вже є «Повернути» для цієї особи.");
        }

        // 5) Контекст для Return: якщо UI не передав — підтягуємо з «відкритого» Dispatch (глобально)
        string? loc = T(item.Location), grp = T(item.GroupName), tool = T(item.ToolType);
        if (item.Type == PlanType.Return && (loc is null || grp is null || tool is null))
        {
            var openDispatch = await _db.PlanElements
                .Where(e => e.PersonId == person.Id && e.Type == PlanType.Dispatch)
                .OrderByDescending(e => e.EventAtUtc)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Останнє відрядження не знайдено.");

            // якщо після нього є будь-яке Return — це не «відкритий» dispatch,
            // але сюди ми б не потрапили через гвард LastActionType == Dispatch.
            loc ??= openDispatch.Location;
            grp ??= openDispatch.GroupName;
            tool ??= openDispatch.ToolType;
        }

        // 6) PPS — фіксуємо стан на момент планування
        var snap = new PlanParticipantSnapshot
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            FullName = person.FullName,
            Rnokpp = (person.Rnokpp ?? string.Empty).Trim(),
            Rank = T(person.Rank),
            PositionSnapshot = person.PositionUnit?.FullName ?? person.PositionUnit?.ShortName,
            Weapon = T(person.Weapon),
            Callsign = T(person.Callsign),
            StatusKindId = person.StatusKindId,
            StatusKindCode = T(person.StatusKind?.Code),
            Author = "system",
            RecordedUtc = DateTime.UtcNow
        };

        // 7) Елемент
        var element = new PlanElement
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = person.Id,
            Type = item.Type,
            EventAtUtc = tUtc,
            Location = loc,
            GroupName = grp,
            ToolType = tool,
            Note = T(item.Note),
            Author = "system",
            RecordedUtc = DateTime.UtcNow,
            PlanParticipantSnapshot = snap
        };

        _db.PlanElements.Add(element);

        if (saveImmediately)
        {
            await _db.SaveChangesAsync(ct);
            await RebuildPlanningAsync(person.Id, ct);
        }

        return element;
    }

    // ---------------- Planning (read-model) ----------------

    /// <summary>
    /// Повністю перебудувати PersonPlanning для особи, спираючись на усі PlanElement та поточний статус Person.
    /// </summary>
    private async Task RebuildPlanningAsync(Guid personId, CancellationToken ct)
    {
        var person = await _db.Persons
            .AsNoTracking()
            .Include(p => p.StatusKind)
            .FirstOrDefaultAsync(p => p.Id == personId, ct)
            ?? throw new InvalidOperationException("Особу не знайдено для оновлення планового стану.");

        // Всі події по особі
        var events = await _db.PlanElements
            .AsNoTracking()
            .Where(e => e.PersonId == personId)
            .OrderBy(e => e.EventAtUtc)
            .ToListAsync(ct);

        var last = events.LastOrDefault();

        // відкритий Dispatch: коли остання дія — Dispatch
        var hasOpen = last?.Type == PlanType.Dispatch;

        // відкритий Dispatch контекст
        PlanElement? openDispatch = null;
        if (hasOpen)
        {
            openDispatch = events.LastOrDefault(e => e.Type == PlanType.Dispatch);
        }

        // найближча бронь у майбутньому
        var now = DateTime.UtcNow;
        var near = events.FirstOrDefault(e => e.EventAtUtc > now);

        var planning = await _db.PersonPlannings.FirstOrDefaultAsync(pp => pp.PersonId == personId, ct);
        if (planning is null)
        {
            planning = new PersonPlanning { Id = Guid.NewGuid(), PersonId = personId };
            _db.PersonPlannings.Add(planning);
        }

        planning.CurrentStatusKindId = person.StatusKindId;
        planning.CurrentStatusKindCode = person.StatusKind?.Code;

        planning.LastActionType = last?.Type;
        planning.LastActionAtUtc = last?.EventAtUtc;

        planning.OpenLocation = hasOpen ? openDispatch?.Location : null;
        planning.OpenGroup = hasOpen ? openDispatch?.GroupName : null;
        planning.OpenTool = hasOpen ? openDispatch?.ToolType : null;

        planning.Author = "system";
        planning.ModifiedUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    // ---------------- Helpers ----------------

    private static bool IsArea(Person p)
    {
        var code = p.StatusKind?.Code;
        var name = p.StatusKind?.Name;
        return string.Equals(code, "30", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, "В районі", StringComparison.OrdinalIgnoreCase);
    }

    private static string? T(string? s)
        => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
