//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanActions
//-----------------------------------------------------------------------------

using eRaven.Application.Services.PlanActionService;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PlanActions;

public partial class PlanActions : ComponentBase, IDisposable
{
    [Inject] public IPlanActionService PlanActionService { get; set; } = default!;

    private string? SearchText;
    private List<PlanAction> Items = [];
    private int Total;
    private PlanAction? Selected;
    private readonly HashSet<Guid> SelectedIds = [];

    // модали
    private bool ShowCreate;
    private bool ShowEdit;
    private bool ShowApprove;
    private Guid? PreselectedPersonId;
    private PlanAction? EditTarget;

    protected override async Task OnInitializedAsync()
    {
        await SearchAsync();
    }

    private async Task SearchAsync()
    {
        var page = await PlanActionService.SearchAsync(SearchText);
        Items = [.. page.Items];
        Total = page.Total;
        await InvokeAsync(StateHasChanged);
    }

    private Task OnSelectedChanged(PlanAction? a)
    {
        Selected = a;
        return Task.CompletedTask;
    }

    private void ToggleSelect(PlanAction a)
    {
        if (SelectedIds.Contains(a.Id)) SelectedIds.Remove(a.Id);
        else SelectedIds.Add(a.Id);
    }

    private void OpenCreateModal()
    {
        PreselectedPersonId = Selected?.PersonId;
        ShowCreate = true;
    }
    private void CloseCreateModal() => ShowCreate = false;

    private void OpenEditModal(PlanAction a)
    {
        EditTarget = a;
        ShowEdit = true;
    }
    private void CloseEditModal() => ShowEdit = false;

    private void ApproveSelected()
    {
        if (SelectedIds.Count == 0) return;
        ShowApprove = true;
    }
    private void CloseApproveModal() => ShowApprove = false;

    private async Task OnModalSaved()
    {
        ShowCreate = false;
        ShowEdit = false;
        await SearchAsync();
    }

    private async Task OnApproved()
    {
        ShowApprove = false;
        SelectedIds.Clear();
        await SearchAsync();
    }

    private async Task DeleteOne(Guid id)
    {
        if (await PlanActionService.DeleteAsync(id))
        {
            Items.RemoveAll(x => x.Id == id);
            StateHasChanged();
        }
    }

    private async Task DeleteSelected()
    {
        foreach (var id in SelectedIds.ToList())
            await DeleteOne(id);
        SelectedIds.Clear();
        await SearchAsync();
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
