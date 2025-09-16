// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// PlansPage
// -----------------------------------------------------------------------------

using Blazored.Toast.Services;
using eRaven.Application.Mappers;
using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Components.Pages.Plans.Modals;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans;

public partial class PlansPage : ComponentBase, IDisposable
{
    [Inject] public IPlanService PlanService { get; set; } = default!;
    [Inject] public IToastService ToastService { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;

    private readonly List<PlanViewModel> _plans = [];
    private PlanViewModel? _selected;

    private bool _busy;
    private bool _loading = true;

    // modal
    private bool _createOpen;
    private CreatePlanModal? _createModal;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            var domain = await PlanService.GetAllPlanAsync();
            _plans.Clear();
            _plans.AddRange(domain.ToViewModels().OrderByDescending(p => p.RecordedUtc));
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally
        {
            _loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private void OpenCreateModal()
    {
        _createOpen = true;
        _createModal?.Open(); // reset внутрішньої моделі модалки
    }

    private Task CloseCreateModal()
    {
        _createOpen = false;
        return Task.CompletedTask;
    }

    private async Task HandleCreated(PlanViewModel created)
    {
        // Після створення — повний reload списку
        await LoadAsync();
        _createOpen = false;
        ToastService.ShowSuccess($"План {created.PlanNumber} створено.");
    }

    private async Task DeleteAsync(Guid id)
    {
        _busy = true;
        try
        {
            var ok = await PlanService.DeleteAsync(id);
            if (ok)
            {
                await LoadAsync();
                if (_selected?.Id == id) _selected = null;
                ToastService.ShowSuccess("План видалено.");
            }
            else
            {
                ToastService.ShowError("Неможливо видалити: план закритий або прив’язаний до наказу.");
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError(ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    // ---- Табличні хендлери ----
    private Task OnSelectedChanged(PlanViewModel? item)
    {
        _selected = item;
        return Task.CompletedTask;
    }

    private void OnRowClick(PlanViewModel item)
    {
        Nav.NavigateTo($"/plans/{item.Id}");
    }

    public void Dispose() => GC.SuppressFinalize(this);
}