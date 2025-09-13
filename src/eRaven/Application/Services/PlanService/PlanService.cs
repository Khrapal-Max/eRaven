// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanService (EF Core; інваріанти прості: план відкритий; 15-хв інтервали;
// чергування дій у межах ПЛАНУ; Return підтягує контекст з останнього Dispatch)
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanService;

public sealed class PlanService(AppDbContext db) : IPlanService
{
    private readonly AppDbContext _db = db;

    // ---------------- Read ----------------

    public async Task<IEnumerable<Plan>> GetAllPlansAsync(CancellationToken ct = default) =>
        await _db.Plans
            .AsNoTracking()
            .OrderByDescending(p => p.RecordedUtc)
            .ToListAsync(ct);

    public async Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct = default) =>
        await _db.Plans
            .AsNoTracking()
            .Include(p => p.PlanElements.OrderBy(e => e.EventAtUtc))
                .ThenInclude(e => e.PlanParticipantSnapshot)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

    // ---------------- Create / Delete plan ----------------

    public async Task<Plan> CreateAsync(CreatePlanViewModel vm, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(vm);
        var number = (vm.PlanNumber ?? string.Empty).Trim();
        if (number.Length == 0) throw new ArgumentException("Номер плану обов’язковий.", nameof(vm.PlanNumber));

        var unique = !await _db.Plans.AnyAsync(p => p.PlanNumber == number, ct);
        if (!unique) throw new InvalidOperationException("План з таким номером уже існує.");

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

    public async Task<bool> DeleteIfOpenAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct);
        if (plan is null) return false;
        if (plan.State != PlanState.Open) throw new InvalidOperationException("План закритий — видалення заборонено.");

        _db.Plans.Remove(plan);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------------- Add / Remove element ----------------

    public async Task<PlanElement> AddElementAsync(Guid planId, CreatePlanElementViewModel item, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Id == planId, ct)
                   ?? throw new InvalidOperationException("План не знайдено.");
        if (plan.State != PlanState.Open) throw new InvalidOperationException("План закритий — редагування заборонено.");

        if (!PlanElement.IsQuarterAligned(item.EventAtUtc))
            throw new InvalidOperationException("Час події має бути 00/15/30/45 хв без секунд.");

        // Персона існує (підтягнемо дані для PPS)
        var person = await _db.Persons
            .Include(x => x.StatusKind)
            .Include(x => x.PositionUnit)
            .FirstOrDefaultAsync(x => x.Id == item.PersonId, ct)
            ?? throw new InvalidOperationException("Особу не знайдено.");

        // Дублікат на той самий момент
        var dup = await _db.PlanElements
            .Where(e => e.PlanId == planId && e.EventAtUtc == item.EventAtUtc)
            .AnyAsync(e => e.PlanParticipantSnapshot.PersonId == item.PersonId, ct);
        if (dup) throw new InvalidOperationException("Вже існує дія для цієї особи на цей час.");

        // Локальний порядок у межах ПЛАНУ (лінійність + чергування типів)
        var timeline = await _db.PlanElements
            .AsNoTracking()
            .Include(e => e.PlanParticipantSnapshot)
            .Where(e => e.PlanId == planId && e.PlanParticipantSnapshot.PersonId == item.PersonId)
            .OrderBy(e => e.EventAtUtc)
            .ToListAsync(ct);

        // Знайдемо сусідів навколо моменту вставки
        var prev = timeline.LastOrDefault(e => e.EventAtUtc <= item.EventAtUtc);
        var next = timeline.FirstOrDefault(e => e.EventAtUtc >= item.EventAtUtc);

        if (prev is not null && prev.EventAtUtc == item.EventAtUtc)
            throw new InvalidOperationException("На цей момент уже є дія.");

        // Чергування: ... D, R, D, R ...
        if (prev is not null && prev.Type == item.Type)
            throw new InvalidOperationException("Дії для особи мають чергуватися в межах плану.");

        if (next is not null && next.Type == item.Type)
            throw new InvalidOperationException("Дії для особи мають чергуватися в межах плану.");

        // Для Return — підтягнемо контекст з найближчого попереднього Dispatch
        string? loc = item.Location, grp = item.GroupName, tool = item.ToolType;
        if (item.Type == PlanType.Return)
        {
            var lastDispatch = timeline.LastOrDefault(e => e.Type == PlanType.Dispatch && e.EventAtUtc < item.EventAtUtc)
                ?? throw new InvalidOperationException("Повернення можливе тільки після відрядження у межах плану.");

            loc = lastDispatch.Location;
            grp = lastDispatch.GroupName;
            tool = lastDispatch.ToolType;
        }

        // PPS
        var pps = new PlanParticipantSnapshot
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            FullName = person.FullName ?? BuildFullName(person),
            Rnokpp = (person.Rnokpp ?? string.Empty).Trim(),
            Rank = person.Rank,
            PositionSnapshot = person.PositionUnit?.FullName ?? person.PositionUnit?.ShortName,
            Weapon = person.Weapon,
            Callsign = person.Callsign,
            StatusKindId = person.StatusKindId,
            StatusKindCode = person.StatusKind?.Code,
            Author = "system",
            RecordedUtc = DateTime.UtcNow
        };

        var el = new PlanElement
        {
            Id = Guid.NewGuid(),
            PlanId = planId,
            Type = item.Type,
            EventAtUtc = item.EventAtUtc,
            Location = T(loc),
            GroupName = T(grp),
            ToolType = T(tool),
            Note = T(item.Note),
            Author = "system",
            RecordedUtc = DateTime.UtcNow,
            PlanParticipantSnapshot = pps
        };

        _db.PlanElements.Add(el);
        await _db.SaveChangesAsync(ct);
        return el;
    }

    public async Task<bool> RemoveElementAsync(Guid planId, Guid elementId, CancellationToken ct = default)
    {
        var planState = await _db.Plans
            .Where(p => p.Id == planId)
            .Select(p => p.State)
            .FirstOrDefaultAsync(ct);

        if (planState == default) throw new InvalidOperationException("План не знайдено.");
        if (planState != PlanState.Open) throw new InvalidOperationException("План закритий — редагування заборонено.");

        var el = await _db.PlanElements.FirstOrDefaultAsync(e => e.Id == elementId && e.PlanId == planId, ct);
        if (el is null) return false;

        _db.PlanElements.Remove(el); // PPS видалиться каскадом
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ---------------- helpers ----------------
    private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static string BuildFullName(dynamic person)
    {
        try
        {
            var full = (string?)person.FullName;
            if (!string.IsNullOrWhiteSpace(full)) return full.Trim();
        }
        catch { /* ignore */ }

        var parts = new[]
        {
            TryStr(person, "LastName"),
            TryStr(person, "FirstName"),
            TryStr(person, "MiddleName")
        }.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim());

        var joined = string.Join(" ", parts);
        return joined.Length > 0 ? joined : "(Невідоме ім'я)";
    }

    private static string? TryStr(object obj, string prop)
        => obj.GetType().GetProperty(prop)?.GetValue(obj) as string;
}
