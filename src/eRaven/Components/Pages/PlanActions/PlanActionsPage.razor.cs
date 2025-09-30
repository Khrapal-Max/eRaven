//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanActionsPage
//-----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Services.PersonService;
using eRaven.Application.Services.PersonStatusService;
using eRaven.Application.Services.PlanActionService;
using eRaven.Application.Services.StatusKindService;
using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Components.Pages.PlanActions.Modals;
using eRaven.Domain.Enums;
using eRaven.Domain.Models;
using FluentValidation;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace eRaven.Components.Pages.PlanActions;

public partial class PlanActionsPage : ComponentBase, IDisposable
{
    // =========================
    // [UI state] візуальний стан
    // =========================
    private CreatePlanActionModal? _createModal;
    private ViewPlanActionModal? _viewModal;
    private ApproveModal? _approveModal;

    protected bool Busy { get; private set; }
    protected string? Search { get; set; }

    protected Person? SelectedPerson { get; set; }
    protected PlanAction? LastPlanAction { get; set; }

    // =========================
    // [Infra] ресурси й токени
    // =========================
    private CancellationTokenSource _cts = new();

    // =========================
    // [Data] джерела та представлення
    // =========================    

    private List<PlanAction> _actions = [];
    private List<Person> _persons = [];
    private List<Person> _filteredPerson = [];
    private List<StatusKind> _statusKinds = [];

    // =========================
    // [DI] сервіси
    // =========================
    [Inject] protected IPlanActionService PlanActionService { get; set; } = default!;
    [Inject] protected IPersonService PersonService { get; set; } = default!;
    [Inject] protected IPersonStatusService PersonStatusService { get; set; } = default!;
    [Inject] protected IStatusKindService StatusKindService { get; set; } = default!;
    [Inject] protected IToastService ToastService { get; set; } = default!;
    [Inject] protected ILogger<PlanActionsPage> Logger { get; set; } = default!;

    // =========================
    // [Lifecycle]
    // =========================
    protected override async Task OnInitializedAsync()
    {
        await LoadPersonAsync();
        await LoadStatusKindAsync();
    }

    // =========================
    // [Public handlers]
    // =========================
    protected async Task LoadStatusKindAsync()
    {
        try
        {
            _statusKinds = [.. await StatusKindService.GetAllAsync()];
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити особові картки"))
            {
                throw;
            }
        }
    }

    protected async Task LoadPersonAsync()
    {
        try
        {
            // 1) Забираємо усі позиції
            _persons = [.. await PersonService.SearchAsync(default, _cts.Token)];

            // 2) Локальний фільтр/сорт/маппінг
            await ApplyFilterAndSort();
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити особові картки"))
            {
                throw;
            }
        }
    }

    /// <summary>Викликається SearchBox після debounce. Тільки локальна рефільтрація.</summary>
    protected async Task OnSearchAsync()
    {
        await ApplyFilterAndSort();
    }

    protected async Task OnPersonClick(Person person)
    {
        SelectedPerson = person;
        _filteredPerson.Clear();

        try
        {
            var actions = await PlanActionService.GetByIdAsync(person.Id, default, _cts.Token);
            _actions = [.. actions.OrderByDescending(x => x?.EffectiveAtUtc)];

            if (_actions.Count > 0)
            {
                LastPlanAction = _actions.First();
            }
            else
            {
                LastPlanAction = null;
            }
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося завантажити планові завдання"))
            {
                throw;
            }
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task CreateAsync(PlanAction planAction)
    {
        try
        {
            await PlanActionService.CreateAsync(planAction);

            await ReloadPlanActionAsync();
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося створити планове завдання"))
            {
                throw;
            }
        }

        await ApplyFilterAndSort();
    }

    private void OpenCreateModal()
    {
        if (SelectedPerson is null) return;

        _createModal?.Open();
    }

    private void OpenApproveModal(PlanAction planAction)
    {
        _approveModal?.Open(planAction);
    }

    // =========================
    // [Helpers]
    // =========================

    private async Task ApplyFilterAndSort()
    {
        SelectedPerson = null;

        _filteredPerson.Clear();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            _filteredPerson = [.. _persons
                .Where(x => x.FullName.Contains(Search)
                         || x.Rnokpp.Contains(Search))];
        }

        await InvokeAsync(StateHasChanged);
    }

    private async Task ApprovePlanActionAsync(ApprovePlanActionViewModel approve)
    {
        try
        {
            var status = approve.MoveType == MoveType.Dispatch ? 2 : 1;

            var ps = new PersonStatus
            {
                Id = approve.Id,
                PersonId = approve!.PersonId,
                StatusKindId = status,
                OpenDate = approve.EffectiveAtUtc,
                IsActive = true,
                Note = approve.Order,
                Author = "import",
                Modified = DateTime.UtcNow
            };

            await PersonStatusService.SetStatusAsync(ps, _cts.Token);

            await PlanActionService.ApproveAsync(approve, _cts.Token);
            ToastService.ShowSuccess("Планове завдання затверджено");

            await ReloadPlanActionAsync();
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося затвердити планове завдання"))
            {
                throw;
            }
        }
    }

    private async Task DeletePlanAction(Guid id)
    {
        try
        {
            await PlanActionService.DeleteAsync(id, _cts.Token);
        }
        catch (Exception ex)
        {
            if (!TryHandleKnownException(ex, "Не вдалося видалити планове завдання"))
            {
                throw;
            }
        }

        await ApplyFilterAndSort();
    }

    private async Task ReloadPlanActionAsync()
    {
        if (SelectedPerson is not null)
        {
            var actions = await PlanActionService.GetByIdAsync(SelectedPerson.Id, default, _cts.Token);
            _actions = [.. actions.OrderByDescending(x => x?.EffectiveAtUtc)];
            LastPlanAction = _actions.FirstOrDefault();
        }
    }

    private bool TryHandleKnownException(Exception ex, string message)
    {
        switch (ex)
        {
            case OperationCanceledException:
                return false;
            case System.ComponentModel.DataAnnotations.ValidationException:
            case FluentValidation.ValidationException:
            case InvalidOperationException:
            case ArgumentException:
            case HttpRequestException:
                ToastService.ShowError($"{message}: {ex.Message}");
                return true;
            default:
                Logger.LogError(ex, "Unexpected error: {Context}", message);
                return false;
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
