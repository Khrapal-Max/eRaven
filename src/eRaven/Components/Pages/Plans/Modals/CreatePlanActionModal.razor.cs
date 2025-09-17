// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanActionModal — code-behind
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PlanActionService;
using eRaven.Application.ViewModels.PersonViewModels;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;
using System.Linq.Expressions;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class CreatePlanActionModal : ComponentBase
{
    [Inject] public IPlanActionService PlanActionService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;

    [Parameter] public Guid PlanId { get; set; }
    [Parameter] public bool ReadOnly { get; set; }
    [Parameter] public EventCallback<PlanActionViewModel> OnCreated { get; set; }

    private bool _open;
    private bool _busy;

    private CreatePlanActionViewModel _model = new();
    private DateTime _eventLocal = DateTime.UtcNow; // прив'язано до input type="datetime-local" (крок 15 хв)
    private string _personQuery = string.Empty;

    private readonly List<PersonEligibilityViewModel> _eligible = [];
    private readonly List<PersonEligibilityViewModel> _ineligible = [];
    private PersonEligibilityViewModel? _selectedPerson;

    public void Open()
    {
        if (ReadOnly) return;

        _model = new() { PlanId = PlanId, EventAtUtc = DateTime.UtcNow, ActionType = PlanActionType.Dispatch };
        _eventLocal = DateTime.UtcNow;
        _personQuery = string.Empty;
        _eligible.Clear();
        _ineligible.Clear();
        _selectedPerson = null;

        _open = true;
        StateHasChanged();
    }

    private async Task CreateAsync()
    {
        if (_busy) return;
        _busy = true;

        try
        {
            // Перетворимо локальний інпут у UTC (крок — 15 хв забезпечує HTML)
            _model.EventAtUtc = DateTime.SpecifyKind(_eventLocal, DateTimeKind.Utc);

            var created = await PlanActionService.CreateAsync(_model);
            await OnCreated.InvokeAsync(created);
            Close();
        }
        catch (InvalidOperationException ex)
        {
            ToastService.ShowError(ex.Message);
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Помилка при створенні дії: " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private void Close() => _open = false;

    private async Task SearchPersonsAsync()
    {
        _eligible.Clear();
        _ineligible.Clear();
        _selectedPerson = null;

        var q = _personQuery?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(q))
        {
            StateHasChanged();
            return;
        }

        var list = await PlanActionService.SearchEligibleAsync(PlanId, _model.ActionType, q, 50);
        foreach (var item in list)
        {
            if (item.IsEligible) _eligible.Add(item);
            else _ineligible.Add(item);
        }

        // Автовибір, якщо рівно один підходить
        if (_eligible.Count == 1)
            await SelectPersonAsync(_eligible[0]);

        StateHasChanged();
    }

    private async Task OnActionTypeChanged(ChangeEventArgs _)
    {
        // Зміна типу дії → перезапустити пошук із тим самим query
        await SearchPersonsAsync();
    }

    private async Task SelectPersonAsync(PersonEligibilityViewModel person)
    {
        if (!person.IsEligible)
        {
            ToastService.ShowWarning(person.IneligibilityReason ?? "Цю особу не можна додати для обраної дії.");
            return;
        }

        _selectedPerson = person;
        _model.PersonId = person.Id;

        // Автозаповнення
        var prefill = await PlanActionService.GetPrefillAsync(
            PlanId,
            person.Id,
            _model.ActionType,
            DateTime.UtcNow
        );

        _model.Location = prefill.Location ?? _model.Location;
        _model.GroupName = prefill.GroupName ?? _model.GroupName;
        _model.CrewName = prefill.CrewName ?? _model.CrewName;

        // Поставимо пропонований час (UTC) у локальний інпут
        _eventLocal = prefill.SuggestedEventAtUtc; // це вже UTC; input покаже yyyy-MM-ddTHH:mm
        StateHasChanged();
    }
}