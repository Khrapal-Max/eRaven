// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlanDetailsPage (code-behind; проста логіка: load -> open modal -> add/remove)
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PlanService;
using eRaven.Application.Services.PersonService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlanDetailsPage : ComponentBase
{
    // -------- Route / DI --------
    [Parameter] public Guid Id { get; set; }

    [Inject] private IPlanService PlanService { get; set; } = default!;
    [Inject] private IPersonService PersonService { get; set; } = default!;
    [Inject] private IToastService Toast { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    // -------- State --------
    private Plan? _plan;
    private readonly List<PlanElement> _elements = [];
    private readonly List<Person> _people = [];

    private bool _busy;
    private bool _modalOpen;

    private bool Editable => _plan?.State == PlanState.Open;

    // -------- Lifecycle --------
    protected override async Task OnInitializedAsync() => await ReloadAsync();

    private async Task ReloadAsync()
    {
        try
        {
            SetBusy(true);

            _plan = await PlanService.GetByIdAsync(Id);
            if (_plan is null)
            {
                Toast.ShowError("План не знайдено.");
                Nav.NavigateTo("/plans");
                return;
            }

            _elements.Clear();
            _elements.AddRange((_plan.PlanElements ?? []).OrderBy(e => e.EventAtUtc));
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося завантажити план. " + ex.Message);
            Nav.NavigateTo("/plans");
        }
        finally
        {
            SetBusy(false);
        }
    }

    // -------- Modal --------
    private async Task OpenModalAsync()
    {
        if (_busy || !Editable) return;

        try
        {
            SetBusy(true);

            // Мінімальний рістер осіб (без додаткових VM)
            _people.Clear();
            var persons = await PersonService.SearchAsync(predicate: null);
            _people.AddRange(persons);
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося підготувати дані. " + ex.Message);
            return;
        }
        finally
        {
            SetBusy(false);
        }

        _modalOpen = true;
        StateHasChanged();
    }

    private Task CloseModalAsync()
    {
        _modalOpen = false;
        return Task.CompletedTask;
    }

    // -------- Add / Remove --------
    private async Task OnModalAddAsync(IReadOnlyList<CreatePlanElementViewModel> items)
    {
        if (_plan is null || items.Count == 0) return;

        try
        {
            SetBusy(true);

            // Батч: або всі, або конкретні помилки як тости (спотворення мінімальне)
            var created = await PlanService.AddElementsAsync(_plan.Id, items);
            Toast.ShowSuccess($"Додано елементів: {created.Count}.");
        }
        catch (InvalidOperationException ex)
        {
            // бізнес-гварди
            Toast.ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося додати елементи. " + ex.Message);
        }
        finally
        {
            SetBusy(false);
            await ReloadAsync();
        }
    }

    private async Task RemoveElementAsync(Guid planId, Guid elementId)
    {
        if (_busy) return;

        try
        {
            SetBusy(true);
            var ok = await PlanService.RemoveElementAsync(planId, elementId);
            if (ok) Toast.ShowSuccess("Елемент видалено.");
            else Toast.ShowWarning("Нічого не змінено (не знайдено).");
        }
        catch (InvalidOperationException ex)
        {
            Toast.ShowWarning(ex.Message);
        }
        catch (Exception ex)
        {
            Toast.ShowError("Не вдалося видалити елемент. " + ex.Message);
        }
        finally
        {
            SetBusy(false);
            await ReloadAsync();
        }
    }

    // -------- Nav / Utils --------
    private void GoBack() => Nav.NavigateTo("/plans");

    private void SetBusy(bool v)
    {
        _busy = v;
        StateHasChanged();
    }
}
