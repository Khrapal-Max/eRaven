//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanMappers
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;

namespace eRaven.Application.Mappers;

public static class PlanMappers
{
    // -------- Domain -> VM --------
    public static PlanViewModel ToViewModel(this Plan p) =>
        new(
            Id: p.Id,
            PlanNumber: p.PlanNumber,
            State: p.State,
            Author: p.Author,
            RecordedUtc: p.RecordedUtc.SpecifyUtc(),
            OrderId: p.OrderId
        );

    public static PlanActionViewModel ToViewModel(this PlanAction a) =>
        new(
            Id: a.Id,
            PlanId: a.PlanId,
            PersonId: a.PersonId,
            ActionType: a.ActionType,
            EventAtUtc: a.EventAtUtc.SpecifyUtc(),
            Location: a.Location,
            GroupName: a.GroupName,
            CrewName: a.CrewName,
            Rnokpp: a.Rnokpp,
            FullName: a.FullName,
            RankName: a.RankName,
            PositionName: a.PositionName,     // посада (штатна назва)
            BZVP: a.BZVP,
            Weapon: string.IsNullOrWhiteSpace(a.Weapon) ? null : a.Weapon,
            Callsign: string.IsNullOrWhiteSpace(a.Callsign) ? null : a.Callsign,
            StatusKindOnDate: a.StatusKindOnDate
        );

    public static PlanDetailsViewModel ToDetailsViewModel(this Plan p) =>
        new(
            Plan: p.ToViewModel(),
            Actions: [.. p.PlanActions
                .OrderBy(x => x.EventAtUtc)
                .Select(x => x.ToViewModel())]
        );

    public static IEnumerable<PlanViewModel> ToViewModels(this IEnumerable<Plan> plans) =>
        plans.Select(p => p.ToViewModel());

    // -------- VM -> Domain --------
    public static Plan ToDomain(this CreatePlanViewModel vm) =>
        new()
        {
            // Id хай виставляє БД або сервіс
            PlanNumber = vm.PlanNumber.Trim(),
            State = PlanState.Open,
            Author = string.IsNullOrWhiteSpace(vm.Author) ? null : vm.Author!.Trim(),
            RecordedUtc = DateTime.UtcNow,
        };

    /// <summary>
    /// Створення/маппінг дії плану із VM (для сценаріїв додавання/редагування).
    /// Якщо передати plan/person — також поставить навігації.
    /// </summary>
    public static PlanAction ToDomain(this PlanActionViewModel vm, Plan? plan = null, Person? person = null) =>
        new()
        {
            Id = vm.Id,
            PlanId = plan?.Id ?? vm.PlanId,
            Plan = plan ?? default!,
            PersonId = person?.Id ?? vm.PersonId,
            Person = person ?? default!,

            ActionType = vm.ActionType,
            EventAtUtc = vm.EventAtUtc.SpecifyUtc(),
            Location = vm.Location.Trim(),
            GroupName = vm.GroupName.Trim(),
            CrewName = vm.CrewName.Trim(),

            Rnokpp = vm.Rnokpp.Trim(),
            FullName = vm.FullName.Trim(),
            RankName = vm.RankName.Trim(),
            PositionName = vm.PositionName.Trim(), // посада
            BZVP = vm.BZVP.Trim(),
            Weapon = vm.Weapon?.Trim() ?? string.Empty,
            Callsign = vm.Callsign?.Trim() ?? string.Empty,
            StatusKindOnDate = vm.StatusKindOnDate.Trim()
        };

    // Додатково: зручний хелпер заповнення снапшоту з Person
    public static PlanAction FillSnapshotFrom(this PlanAction a, Person p, string statusOnDate)
    {
        a.Rnokpp = p.Rnokpp;
        a.FullName = p.FullName;
        a.RankName = p.Rank;
        a.PositionName = p.PositionUnit?.FullName ?? string.Empty; // посада
        a.BZVP = p.BZVP;
        a.Weapon = p.Weapon ?? string.Empty;
        a.Callsign = p.Callsign ?? string.Empty;
        a.StatusKindOnDate = statusOnDate;
        return a;
    }

    // Утиліти
    private static DateTime SpecifyUtc(this DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
}
