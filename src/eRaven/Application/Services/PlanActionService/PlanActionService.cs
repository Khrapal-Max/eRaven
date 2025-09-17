//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionService
//-----------------------------------------------------------------------------

using eRaven.Application.Mappers;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using eRaven.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Application.Services.PlanActionService;

public sealed class PlanActionService(AppDbContext db) : IPlanActionService
{
    private readonly AppDbContext _db = db;

    public async Task<IReadOnlyList<PersonEligibilityViewModel>> SearchEligibleAsync(
    Guid planId,
    PlanActionType actionType,
    string query,
    int take = 50,
    CancellationToken ct = default)
    {
        query = (query ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<PersonEligibilityViewModel>();

        // 1) Кандидати за рядком пошуку
        var people = await _db.Persons
            .Include(p => p.PositionUnit)
            .AsNoTracking()
            .Where(p =>
                p.Rnokpp.Contains(query) ||
                p.LastName.Contains(query) ||
                p.FirstName.Contains(query) ||
                (p.MiddleName != null && p.MiddleName.Contains(query)) ||
                (p.Callsign != null && p.Callsign.Contains(query))
            )
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ThenBy(p => p.MiddleName)
            .Take(take)
            .ToListAsync(ct);

        if (people.Count == 0) return Array.Empty<PersonEligibilityViewModel>();
        var personIds = people.Select(p => p.Id).ToArray();

        // 2) Остання дія цієї особи в ЦЬОМУ плані: використовуємо NOT EXISTS замість GroupBy/First
        var lastInThisPlan = await _db.PlanActions
            .AsNoTracking()
            .Where(a => a.PlanId == planId && personIds.Contains(a.PersonId))
            .Where(a => !_db.PlanActions.Any(b =>
                b.PlanId == a.PlanId &&
                b.PersonId == a.PersonId &&
                b.EventAtUtc > a.EventAtUtc))
            .Select(a => new { a.PersonId, a.ActionType, a.EventAtUtc })
            .ToDictionaryAsync(x => x.PersonId, x => (x.ActionType, x.EventAtUtc), ct);

        // 3) Останні дії в ІНШИХ відкритих планах (де остання = Dispatch)
        //    Також через NOT EXISTS; PlanNumber підтягуємо тут же (ще без агрегацій)
        var lastOpenElsewhere = await _db.PlanActions
            .AsNoTracking()
            .Where(a => a.PlanId != planId &&
                        a.Plan.State == Domain.Enums.PlanState.Open &&
                        a.Plan.OrderId == null &&
                        personIds.Contains(a.PersonId))
            .Where(a => !_db.PlanActions.Any(b =>
                b.PlanId == a.PlanId &&
                b.PersonId == a.PersonId &&
                b.EventAtUtc > a.EventAtUtc))
            .Where(a => a.ActionType == PlanActionType.Dispatch)
            .Select(a => new { a.PersonId, a.PlanId, a.Plan.PlanNumber })
            .ToListAsync(ct);

        // Для швидкого пошуку — згрупуємо в пам'яті за PersonId (беремо будь-який план)
        var activeElsewhere = lastOpenElsewhere
            .GroupBy(x => x.PersonId)
            .ToDictionary(g => g.Key, g => g.First());

        // 4) Формуємо відповідь
        var result = new List<PersonEligibilityViewModel>(people.Count);

        foreach (var p in people)
        {
            lastInThisPlan.TryGetValue(p.Id, out var lastHere);
            var hasElsewhere = activeElsewhere.TryGetValue(p.Id, out var elsewhere);

            bool eligible;
            string? reason = null;

            if (actionType == PlanActionType.Dispatch)
            {
                if (lastHere != default)
                {
                    eligible = false;
                    reason = "Вже є дія в цьому плані.";
                }
                else if (hasElsewhere)
                {
                    eligible = false;
                    reason = $"Задіяний в іншому відкритому плані ({elsewhere!.PlanNumber}).";
                }
                else
                {
                    eligible = true;
                }
            }
            else // Return
            {
                if (lastHere != default && lastHere.ActionType == PlanActionType.Dispatch)
                {
                    eligible = true;
                }
                else
                {
                    eligible = false;
                    reason = (lastHere == default)
                        ? "Немає відрядження у цьому плані."
                        : "Остання дія не 'Відрядити'.";
                }
            }

            result.Add(new PersonEligibilityViewModel(
                p.Id,
                p.FullName,
                p.Rnokpp,
                p.Rank,
                p.PositionUnit?.FullName ?? string.Empty,
                p.BZVP,
                p.Weapon,
                p.Callsign,
                eligible,
                reason
            ));
        }

        return result;
    }

    public async Task<PlanActionPrefillViewModel> GetPrefillAsync(
        Guid planId,
        Guid personId,
        PlanActionType actionType,
        DateTime nowUtc,
        CancellationToken ct = default)
    {
        // 1) Спробуємо взяти останню дію цієї особи в межах цього плану
        var lastHere = await _db.PlanActions
            .Where(a => a.PlanId == planId && a.PersonId == personId)
            .OrderByDescending(a => a.EventAtUtc)
            .Select(a => new
            {
                a.ActionType,
                a.EventAtUtc,
                a.Location,
                a.GroupName,
                a.CrewName
            })
            .FirstOrDefaultAsync(ct);

        // 2) Підказки: якщо є контекст у цьому плані — підставимо
        string? loc = lastHere?.Location;
        string? grp = lastHere?.GroupName;
        string? crw = lastHere?.CrewName;

        // 3) Запропонуємо час: найближчий “квартал” >= nowUtc і > останньої дії (якщо є)
        var suggested = SnapToNextQuarter(nowUtc);
        if (lastHere?.EventAtUtc is DateTime lastAt && lastAt >= suggested)
        {
            // підсунемо на наступний “квартал” після останнього запису
            suggested = SnapToNextQuarter(lastAt.AddMinutes(1));
        }

        return new PlanActionPrefillViewModel(loc, grp, crw, suggested);
    }


    public async Task<IReadOnlyList<PlanActionViewModel>> GetByPlanAsync(Guid planId, CancellationToken ct = default)
    {
        // достатньо snapshot-полів; навігації не потрібні
        var actions = await _db.PlanActions
            .AsNoTracking()
            .Where(a => a.PlanId == planId)
            .OrderBy(a => a.EventAtUtc)
            .ToListAsync(ct);

        return actions.ToViewModels();
    }

    public async Task<PlanActionViewModel> CreateAsync(CreatePlanActionViewModel vm, CancellationToken ct = default)
    {
        // 1) Перевіряємо план
        var plan = await _db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == vm.PlanId, ct);

        ArgumentNullException.ThrowIfNull(plan, nameof(plan));

        if (plan.State != PlanState.Open || plan.OrderId is not null)
            throw new InvalidOperationException("Додавати дії можна лише до відкритого плану без наказу.");

        // 2) Перевіряємо особу
        var person = await _db.Persons
            .Include(p => p.PositionUnit)
            .Include(p => p.StatusKind)
            .FirstOrDefaultAsync(p => p.Id == vm.PersonId, ct);

        ArgumentNullException.ThrowIfNull(person, nameof(person));

        // 3) Перевіряємо, чи особа вже має дії в цьому плані
        var lastAction = await _db.PlanActions
            .Where(a => a.PlanId == plan.Id && a.PersonId == person.Id)
            .OrderByDescending(a => a.EventAtUtc)
            .FirstOrDefaultAsync(ct);

        if (lastAction is null)
        {
            // Якщо спроба додати повернення без жодного відрядження
            if (vm.ActionType == PlanActionType.Return)
                throw new InvalidOperationException("Неможливо додати повернення без попереднього відрядження.");
        }
        else
        {
            if (lastAction.ActionType == PlanActionType.Dispatch)
            {
                if (vm.ActionType == PlanActionType.Dispatch)
                    throw new InvalidOperationException("Особа вже додана до плану (очікує повернення).");
                // Note: якщо ActionType == Return, дозволяємо (закриваємо відрядження)
            }
            else if (lastAction.ActionType == PlanActionType.Return)
            {
                // Остання дія вже була повернення – цикл завершено
                throw new InvalidOperationException("Особа вже виконала відрядження і повернення у цьому плані.");
            }
        }

        // 4) Перевіряємо, що особа не задіяна в іншому відкритому плані
        bool openConflict = await _db.PlanActions.AnyAsync(a =>
            a.PersonId == person.Id &&
            a.PlanId != plan.Id &&
            a.Plan.State == PlanState.Open &&
            a.Plan.OrderId == null, ct);

        if (openConflict)
            throw new InvalidOperationException("Особа наразі задіяна в іншому відкритому плані.");

        // 5) Нормалізуємо час у UTC
        var eventAtUtc = vm.EventAtUtc.Kind == DateTimeKind.Utc
            ? vm.EventAtUtc
            : DateTime.SpecifyKind(vm.EventAtUtc, DateTimeKind.Utc);


        // 6) Створюємо дію зі знімком Person
        var action = new PlanAction
        {
            Id = Guid.NewGuid(),
            PlanId = plan.Id,
            PersonId = person.Id,

            ActionType = vm.ActionType,
            EventAtUtc = eventAtUtc,
            Location = vm.Location,
            GroupName = vm.GroupName,
            CrewName = vm.CrewName,

            Rnokpp = person.Rnokpp,
            FullName = person.FullName,
            RankName = person.Rank,
            PositionName = person.PositionUnit?.FullName ?? string.Empty,
            BZVP = person.BZVP,
            Weapon = person.Weapon ?? string.Empty,
            Callsign = person.Callsign ?? string.Empty,
            StatusKindOnDate = person.StatusKind?.Name ?? string.Empty
        };

        await _db.PlanActions.AddAsync(action, ct);
        await _db.SaveChangesAsync(ct);

        return action.ToViewModel();
    }

    public async Task<bool> DeleteAsync(Guid planActionId, CancellationToken ct = default)
    {
        // Потрібно перевірити стан плану: якщо план закритий або вже прив’язаний до наказу — заборонити
        var action = await _db.PlanActions
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == planActionId, ct);

        if (action is null) return false;

        if (action.Plan.State != PlanState.Open || action.Plan.OrderId is not null)
            return false;

        _db.PlanActions.Remove(action);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ===== Helpers =====
    private static DateTime SnapToNextQuarter(DateTime dtUtc)
    {
        if (dtUtc.Kind != DateTimeKind.Utc)
            dtUtc = DateTime.SpecifyKind(dtUtc, DateTimeKind.Utc);

        var minute = dtUtc.Minute;
        var mod = minute % 15;
        if (mod == 0 && dtUtc.Second == 0 && dtUtc.Millisecond == 0)
            return dtUtc;

        var add = 15 - mod;
        var snapped = new DateTime(dtUtc.Year, dtUtc.Month, dtUtc.Day, dtUtc.Hour, minute, 0, DateTimeKind.Utc)
            .AddMinutes(add);
        return new DateTime(snapped.Year, snapped.Month, snapped.Day, snapped.Hour, snapped.Minute, 0, DateTimeKind.Utc);
    }
}
