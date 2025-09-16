//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// OrderMappers
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.OrderViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Mappers;

public static class OrderMappers
{
    // -------- Domain -> VM --------
    public static OrderViewModel ToViewModel(this Order o) =>
        new(
            Id: o.Id,
            Name: o.Name,
            EffectiveMomentUtc: o.EffectiveMomentUtc.SpecifyUtc(),
            Author: o.Author,
            RecordedUtc: o.RecordedUtc.SpecifyUtc()
        );

    public static OrderActionViewModel ToViewModel(this OrderAction a) =>
        new(
            Id: a.Id,
            OrderId: a.OrderId,
            PlanId: a.PlanId,
            PlanActionId: a.PlanActionId,
            PersonId: a.PersonId,
            ActionType: a.ActionType,
            EventAtUtc: a.EventAtUtc.SpecifyUtc(),
            Location: a.Location,
            GroupName: a.GroupName,
            CrewName: a.CrewName,
            Rnokpp: a.Rnokpp,
            FullName: a.FullName,
            RankName: a.RankName,
            PositionName: a.PositionName, // посада
            BZVP: a.BZVP,
            Weapon: string.IsNullOrWhiteSpace(a.Weapon) ? null : a.Weapon,
            Callsign: string.IsNullOrWhiteSpace(a.Callsign) ? null : a.Callsign,
            StatusKindOnDate: a.StatusKindOnDate
        );

    public static OrderDetailsViewModel ToDetailsViewModel(this Order o) =>
        new(
            Order: o.ToViewModel(),
            PlanIds: [.. o.Plans.Select(p => p.Id)],
            Actions: [.. o.Actions
                .OrderBy(x => x.EventAtUtc)
                .Select(x => x.ToViewModel())]
        );

    public static IEnumerable<OrderViewModel> ToViewModels(this IEnumerable<Order> orders) =>
        orders.Select(o => o.ToViewModel());

    /// <summary>
    /// Для відповіді після публікації наказу.
    /// </summary>
    public static ExecutedPublishDailyOrderViewModel ToExecutedViewModel(
        this Order o,
        IEnumerable<Plan> closedPlans,
        IEnumerable<OrderAction> confirmedActions)
        => new(
            Order: o.ToViewModel(),
            ClosedPlanIds: [.. closedPlans.Select(p => p.Id)],
            ConfirmedActions: [.. confirmedActions
                .OrderBy(a => a.EventAtUtc)
                .Select(a => a.ToViewModel())]
        );

    // -------- VM -> Domain --------
    public static Order ToDomain(this CreatePublishDailyOrderViewModel vm) =>
        new()
        {
            Name = vm.Name.Trim(),
            EffectiveMomentUtc = vm.EffectiveMomentUtc.SpecifyUtc(),
            Author = string.IsNullOrWhiteSpace(vm.Author) ? null : vm.Author!.Trim(),
            // Plans / Actions заповнюються у сервісі публікації
        };

    public static OrderAction ToDomain(this OrderActionViewModel vm, Order? order = null) =>
        new()
        {
            Id = vm.Id,
            OrderId = order?.Id ?? vm.OrderId,
            Order = order ?? default!,
            PlanId = vm.PlanId,
            PlanActionId = vm.PlanActionId,
            PersonId = vm.PersonId,

            ActionType = vm.ActionType,
            EventAtUtc = vm.EventAtUtc.SpecifyUtc(),
            Location = vm.Location.Trim(),
            GroupName = vm.GroupName.Trim(),
            CrewName = vm.CrewName.Trim(),

            Rnokpp = vm.Rnokpp.Trim(),
            FullName = vm.FullName.Trim(),
            RankName = vm.RankName.Trim(),
            PositionName = vm.PositionName.Trim(),
            BZVP = vm.BZVP.Trim(),
            Weapon = vm.Weapon?.Trim() ?? string.Empty,
            Callsign = vm.Callsign?.Trim() ?? string.Empty,
            StatusKindOnDate = vm.StatusKindOnDate.Trim()
        };

    // Утиліти
    private static DateTime SpecifyUtc(this DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
}
