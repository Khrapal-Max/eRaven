// -----------------------------------------------------------------------------
// PlanDetailsPage (code-behind)
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Application.Services.PersonService; // припускаю існує
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlanDetailsPage : ComponentBase
{
    [Parameter] public Guid Id { get; set; }

    [Inject] private IPlanService PlanService { get; set; } = default!;
    [Inject] private IPersonService PersonService { get; set; } = default!; // для ростера
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private Plan? _plan;
    private readonly List<PlanElement> _elements = [];
    private bool _busy;
    private bool Editable => _plan?.State == PlanState.Open;

    private bool _modalOpen;
    private readonly List<PlanRosterViewModel> _roster = [];

    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            _busy = true;
            _plan = await PlanService.GetByIdAsync(Id);
            if (_plan is null) { Toast.ShowError("План не знайдено."); Nav.NavigateTo("/plans"); return; }

            _elements.Clear();
            _elements.AddRange((_plan.PlanElements ?? []).OrderBy(e => e.EventAtUtc));
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося завантажити план. " + ex.Message);
            Nav.NavigateTo("/plans");
        }
        finally { _busy = false; StateHasChanged(); }
    }

    private async Task OpenModalAsync()
    {
        if (_busy || _plan is null) return;

        try
        {
            _busy = true;

            // Підтягнемо активних людей (у прикладі без предиката: заберемо всіх і відсортуємо)
            var persons = await PersonService.SearchAsync(predicate: null);
            _roster.Clear();
            _roster.AddRange(persons.Select(p => new PlanRosterViewModel
            {
                PersonId = p.Id,
                FullName = p.FullName ?? string.Join(" ", new[] { p.LastName, p.FirstName, p.MiddleName }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Rnokpp = p.Rnokpp,
                Rank = p.Rank,
                Position = p.PositionUnit?.FullName ?? p.PositionUnit?.ShortName,
                StatusKindCode = p.StatusKind?.Code,
                StatusKindName = p.StatusKind?.Name
            }));
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося завантажити список осіб. " + ex.Message);
            return;
        }
        finally { _busy = false; }

        _modalOpen = true;
        StateHasChanged();
    }

    private Task CloseModalAsync() { _modalOpen = false; return Task.CompletedTask; }

    private async Task OnModalAddAsync(IReadOnlyList<CreatePlanElementViewModel> items)
    {
        if (_plan is null || items.Count == 0) return;

        try
        {
            _busy = true;

            var ok = 0;
            foreach (var it in items)
            {
                try { await PlanService.AddElementAsync(_plan.Id, it); ok++; }
                catch (Exception ex) { Toast.ShowWarning($"PersonId={it.PersonId}: {ex.Message}"); }
            }

            if (ok > 0) Toast.ShowSuccess($"Додано елементів: {ok}.");
        }
        finally
        {
            _busy = false;
            await ReloadAsync();
        }
    }

    private async Task RemoveElementAsync(Guid planId, Guid elementId)
    {
        if (_busy) return;
        try
        {
            _busy = true;
            var changed = await PlanService.RemoveElementAsync(planId, elementId);
            if (changed) Toast.ShowSuccess("Елемент видалено.");
            else Toast.ShowWarning("Нічого не змінено.");
        }
        catch (InvalidOperationException ex) { Toast.ShowWarning(ex.Message); }
        catch (Exception ex) { Toast.ShowError("Не вдалося видалити елемент. " + ex.Message); }
        finally { _busy = false; await ReloadAsync(); }
    }

    private void GoBack() => Nav.NavigateTo("/plans");
}
