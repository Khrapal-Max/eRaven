//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActionModal
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PlanActions.Modals;

public partial class PlanActionModal : ComponentBase
{
    [Inject] public IPlanActionService PlanActionService { get; set; } = default!;

    [Parameter] public string Title { get; set; } = "Дія";
    [Parameter] public Guid? PersonId { get; set; } // для “Додати”
    [Parameter] public Guid? ActionId { get; set; } // для “Редагувати”
    [Parameter] public EventCallback OnSaved { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    private bool IsEdit => ActionId.HasValue;
    private CreatePlanActionViewModel Vm = new();
    private string PersonRaw = string.Empty;
    private string TripRaw = string.Empty;

    private DateTime LocalDate
    {
        get => Vm.EffectiveAtUtc.ToLocalTime();
        set => Vm.EffectiveAtUtc = DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
    }

    protected override async Task OnInitializedAsync()
    {
        if (IsEdit)
        {
            var a = await PlanActionService.GetByIdAsync(ActionId!.Value);
            if (a is null) { await OnCancel.InvokeAsync(); return; }

            // редагувати дозволено лише чернетки
            if (a.ActionState != ActionState.PlanAction) { await OnCancel.InvokeAsync(); return; }

            PersonRaw = a.PersonId.ToString();
            TripRaw = a.TripId?.ToString() ?? "";
            Vm = new()
            {
                PersonId = a.PersonId,
                MoveType = a.MoveType,
                ToStatusKindId = a.ToStatusKindId,
                EffectiveAtUtc = a.EffectiveAtUtc,
                TripId = a.TripId,
                Location = a.Location,
                GroupName = a.GroupName,
                CrewName = a.CrewName,
                Note = a.Note
            };
        }
        else
        {
            PersonRaw = PersonId?.ToString() ?? "";
            Vm.PersonId = PersonId ?? Guid.Empty;
            Vm.ToStatusKindId = 2; // зручно
            Vm.EffectiveAtUtc = DateTime.UtcNow;
        }
    }

    private async Task SaveAsync()
    {
        // мінімальна нормалізація
        Vm.TripId = string.IsNullOrWhiteSpace(TripRaw) ? null : (Guid.TryParse(TripRaw, out var t) ? t : null);
        if (!Guid.TryParse(PersonRaw, out var pid)) return;
        Vm.PersonId = pid;

        if (!IsEdit)
        {
            // create
            var dto = new CreatePlanActionDto(
              Vm.PersonId, Vm.MoveType, Vm.ToStatusKindId, Vm.EffectiveAtUtc,
              Vm.TripId, Vm.Location, Vm.GroupName, Vm.CrewName, Vm.Note
            );
            await PlanActionService.AddActionAsync(dto);
        }
        else
        {
            // update draft (без зміни MoveType/TripId — простіше і безпечно)
            var update = new UpdatePlanActionDto(
              ActionId!.Value, Vm.EffectiveAtUtc, Vm.ToStatusKindId, Vm.Location, Vm.GroupName, Vm.CrewName, Vm.Note
            );
            await PlanActionService.UpdateDraftAsync(update);
        }

        await OnSaved.InvokeAsync();
    }
}
