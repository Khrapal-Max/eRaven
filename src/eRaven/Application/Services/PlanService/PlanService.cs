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

        if (vm.PlanElements is null || vm.PlanElements.Count == 0)
            throw new InvalidOperationException("План має містити принаймні один елемент.");

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

    // ------------------------------- Update ---------------------------------

    /// <summary>
    /// Якщо план відкритий і без наказу — повністю перезаписуємо елементи:
    /// видаляємо чинні та додаємо ті, що прийшли в <paramref name="incoming"/>.
    /// </summary>
    public async Task<bool> UpdateIfOpenAsync(Plan incoming, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        if (incoming.Id == Guid.Empty) throw new ArgumentException("plan.Id is required.", nameof(incoming));

        var plan = await _db.Plans
            .Include(p => p.PlanElements)
                .ThenInclude(e => e.Participants)
            .FirstOrDefaultAsync(p => p.Id == incoming.Id, ct);

        if (plan is null) return false;

        if (plan.State != PlanState.Open)
            throw new InvalidOperationException("План закритий — редагування заборонено.");

        var hasOrder = await _db.Orders.AnyAsync(o => o.PlanId == plan.Id, ct);
        if (hasOrder)
            throw new InvalidOperationException("План має наказ — редагування заборонено.");

        if (string.IsNullOrWhiteSpace(incoming.PlanNumber))
            throw new InvalidOperationException("PlanNumber обовʼязковий.");

        plan.PlanNumber = incoming.PlanNumber.Trim();
        plan.Author = Normalize(incoming.Author) ?? plan.Author;

        if (incoming.PlanElements is null || incoming.PlanElements.Count == 0)
            throw new InvalidOperationException("План має містити принаймні один елемент.");

        // 1) Видаляємо ЛИШЕ елементи (учасники підуть каскадом) — уникаємо подвійних DELETE
        _db.PlanElements.RemoveRange(plan.PlanElements);
        plan.PlanElements.Clear();
        await _db.SaveChangesAsync(ct); // флеш для стабільності

        // 2) Додаємо нові елементи (нормалізовані) одним махом
        var fresh = incoming.PlanElements.Select(e => NormalizeElementForUpdate(e, plan.Id)).ToList();
        _db.PlanElements.AddRange(fresh);

        await _db.SaveChangesAsync(ct);
        return true;
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
