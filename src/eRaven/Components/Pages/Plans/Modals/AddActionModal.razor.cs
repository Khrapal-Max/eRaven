/*// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanViewModelValidator
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class AddActionModal : ComponentBase
{
    [Parameter] public string PlanNumber { get; set; } = default!;
    [Parameter] public Guid? PreselectedPersonId { get; set; }
    [Parameter] public EventCallback OnSaved { get; set; }

    [Inject] protected IPersonService PersonService { get; set; } = default!;
    [Inject] protected IPlanService PlanService { get; set; } = default!;
    [Inject] protected IToastService ToastService { get; set; } = default!;

    protected bool _open;
    protected bool _busy;

    public PlanActionViewModel Model { get; private set; } = new();
    protected List<Person> _people = [];
    protected DateTime _whenLocal = DateTime.Now;

    private CancellationTokenSource? _cts;

    public async Task OpenAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        _busy = false;
        _open = true;

        // ініціалізуємо форму, беручи значення з параметрів
        Model = new PlanActionViewModel
        {
            PlanNumber = PlanNumber ?? string.Empty,
            PersonId = PreselectedPersonId ?? Guid.Empty,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.Now,      // локальний; сервіс переведе в UTC
            Location = string.Empty,
            GroupName = string.Empty,
            CrewName = string.Empty,
            Note = null
        };

        _whenLocal = DateTime.Now;

        // лише через сервіс
        _people = [.. (await PersonService.SearchAsync(null, _cts.Token))];

        await InvokeAsync(StateHasChanged);
    }

    protected void Cancel()
    {
        _cts?.Cancel();
        _open = false;
        StateHasChanged();
    }

    protected async Task SubmitAsync()
    {
        Model.EventAtUtc = DateTime.SpecifyKind(_whenLocal, DateTimeKind.Local);

        try
        {
            _busy = true;
            await PlanService.AddActionAndApplyStatusAsync(Model, author: "ui", ct: _cts?.Token ?? default);
            _open = false;
            await OnSaved.InvokeAsync();
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
            await InvokeAsync(StateHasChanged);
        }
    }
}*/