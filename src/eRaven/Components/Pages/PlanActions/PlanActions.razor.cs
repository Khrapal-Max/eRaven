//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActions
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PlanActions;

public partial class PlanActions : ComponentBase, IDisposable
{
    [Inject] public IPlanActionService PlanActionService { get; set; } = default!;
    [Inject] public IValidator<CreatePlanActionViewModel> CreateValidator { get; set; } = default!;
    [Inject] public IValidator<ApproveOptionsViewModel> ApproveValidator { get; set; } = default!;

    // state
    private Guid? PersonId;
    private string PersonIdRaw { get; set; } = string.Empty;
    private string TripIdRaw { get; set; } = string.Empty;

    private CreatePlanActionViewModel CreateModel = new();
    private readonly ApproveOptionsViewModel ApproveModel = new();

    private List<PlanAction> Items { get; set; } = [];
    private PlanAction? Selected { get; set; }

    // helpers for datetime-local (uses local time; we store UTC)
    private DateTime EffectiveLocal
    {
        get => DateTime.SpecifyKind(CreateModel.EffectiveAtUtc, DateTimeKind.Utc).ToLocalTime();
        set => CreateModel.EffectiveAtUtc = DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
    }

    private bool CanCreate => PersonId.HasValue;
    private bool CanApprove => ApproveModel.SelectedActionIds.Count > 0;

    protected override void OnInitialized()
    {
        ResetCreate();
    }

    private async Task LoadForPerson()
    {
        Selected = null;
        Items.Clear();
        ApproveModel.SelectedActionIds.Clear();

        if (!Guid.TryParse(PersonIdRaw, out var pid))
            return;

        PersonId = pid;
        CreateModel.PersonId = pid;

        Items = [.. (await PlanActionService.GetByPersonAsync(pid, onlyDraft: false))];
        StateHasChanged();
    }

    private void OnMoveTypeChanged()
    {
        // зручність: підказати ToStatusKindId
        if (CreateModel.MoveType == MoveType.Dispatch && CreateModel.ToStatusKindId <= 0)
            CreateModel.ToStatusKindId = 2; // "В БР" за замовчуванням
        if (CreateModel.MoveType == MoveType.Return && CreateModel.ToStatusKindId <= 0)
            CreateModel.ToStatusKindId = 1; // "В районі" за замовчуванням
    }

    private async Task CreateAsync()
    {
        var vr = await CreateValidator.ValidateAsync(CreateModel);
        if (!vr.IsValid) return;

        // нормалізувати TripId
        CreateModel.TripId = string.IsNullOrWhiteSpace(TripIdRaw) ? null :
            Guid.TryParse(TripIdRaw, out var t) ? t : null;

        var dto = new CreatePlanActionDto(
            CreateModel.PersonId,
            CreateModel.MoveType,
            CreateModel.ToStatusKindId,
            CreateModel.EffectiveAtUtc,
            CreateModel.TripId,
            CreateModel.Location,
            CreateModel.GroupName,
            CreateModel.CrewName,
            CreateModel.Note
        );

        var created = await PlanActionService.AddActionAsync(dto);
        Items.Add(created);
        Items = [.. Items.OrderBy(i => i.EffectiveAtUtc)];

        ResetCreate();
        StateHasChanged();
    }

    private void ResetCreate()
    {
        CreateModel = new()
        {
            PersonId = PersonId ?? Guid.Empty,
            MoveType = MoveType.Dispatch,
            ToStatusKindId = 2, // зручно за замовчуванням
            EffectiveAtUtc = DateTime.UtcNow,
            TripId = null,
            Location = string.Empty,
            GroupName = string.Empty,
            CrewName = string.Empty,
            Note = null
        };
        TripIdRaw = string.Empty;
    }

    private void OnRowClick(PlanAction a)
    {
        // керування множинним вибором для approve (простий варіант: toggle)
        if (ApproveModel.SelectedActionIds.Contains(a.Id))
            ApproveModel.SelectedActionIds.Remove(a.Id);
        else
            ApproveModel.SelectedActionIds.Add(a.Id);
    }

    private Task OnSelectChanged(PlanAction? a)
    {
        Selected = a;
        return Task.CompletedTask;
    }

    private async Task DeleteSelectedAsync()
    {
        if (Selected is null) return;
        var ok = await PlanActionService.DeleteAsync(Selected.Id);
        if (ok)
        {
            Items.RemoveAll(x => x.Id == Selected.Id);
            Selected = null;
            await InvokeAsync(StateHasChanged);
        }
        // (опціонально: показати toast)
    }

    private async Task ApproveSelectedAsync()
    {
        var vr = await ApproveValidator.ValidateAsync(ApproveModel);
        if (!vr.IsValid) return;

        // пакетний апрув
        var result = await PlanActionService.ApproveBatchAsync(
            ApproveModel.SelectedActionIds,
            new ApproveOptions(ApproveModel.OrderName, ApproveModel.Author)
        );

        // оновити локальний список: перевести затверджені в ApprovedOrder + проставити OrderName
        var approvedIds = result.PerAction.Where(r => r.Applied).Select(r => r.ActionId).ToHashSet();
        foreach (var a in Items.Where(i => approvedIds.Contains(i.Id)))
        {
            a.ActionState = ActionState.ApprovedOrder;
            a.Order = ApproveModel.OrderName;
        }

        ApproveModel.SelectedActionIds.Clear();
        await InvokeAsync(StateHasChanged);
        // (опціонально: показати summary успіхів/помилок)
    }

    public void Dispose()
    {
        ApproveModel.SelectedActionIds.Clear();
        GC.SuppressFinalize(this);
    }
}
