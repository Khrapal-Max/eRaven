// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanViewModelValidator
// -----------------------------------------------------------------------------

using Blazored.FluentValidation;
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
    [Parameter] public EventCallback OnSaved { get; set; }

    [Inject] protected IPersonService PersonService { get; set; } = default!;
    [Inject] protected IPlanService PlanService { get; set; } = default!;
    [Inject] protected IToastService Toasts { get; set; } = default!;

    // UI state
    protected bool _open;
    protected bool _busy;

    // форма
    public PlanActionViewModel Model { get; private set; } = new();
    protected List<Person> _people = [];
    protected FluentValidationValidator? _fv;

    // локальний час з інпута
    protected DateTime _whenLocal = DateTime.Now;

    private CancellationTokenSource? _cts;

    /// <summary>
    /// Відкрити модалку й одразу встановити план і (необов’язково) попередньо обрану особу.
    /// </summary>
    public async Task OpenAsync(string planNumber, Guid? preselectedPersonId = null)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        _busy = false;
        _open = true;

        Model = new PlanActionViewModel
        {
            PlanNumber = planNumber,
            PersonId = preselectedPersonId ?? Guid.Empty,
            ActionType = PlanActionType.Dispatch,
            EventAtUtc = DateTime.Now,   // перед сабмітом замінимо на _whenLocal (Local)
            Location = string.Empty,
            GroupName = string.Empty,
            CrewName = string.Empty,
            Note = null
        };

        _whenLocal = DateTime.Now;

        // людей беремо тільки через сервіс
        _people = [.. await PersonService.SearchAsync(null, _cts.Token)];

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
        if (!await _fv!.ValidateAsync()) return;

        Model.EventAtUtc = DateTime.SpecifyKind(_whenLocal, DateTimeKind.Local);

        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        try
        {
            _busy = true;

            // легка нормалізація
            Model.Location = Model.Location?.Trim() ?? string.Empty;
            Model.GroupName = Model.GroupName?.Trim() ?? string.Empty;
            Model.CrewName = Model.CrewName?.Trim() ?? string.Empty;
            Model.Note = string.IsNullOrWhiteSpace(Model.Note) ? null : Model.Note.Trim();

            await PlanService.AddActionAndApplyStatusAsync(Model, author: "ui", ct: _cts?.Token ?? default);

            _open = false;
            await OnSaved.InvokeAsync();
        }
        catch (Exception ex)
        {
            // бізнес-інваріанти (Return без Dispatch тощо) приїдуть сюди
            Toasts.ShowError(ex.Message);
        }

        _open = false;
        _busy = false;
        await InvokeAsync(StateHasChanged);
    }
}