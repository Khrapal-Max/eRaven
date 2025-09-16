// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanModal
// -----------------------------------------------------------------------------

using eRaven.Application.Services.PlanService;
using eRaven.Application.ViewModels.PlanViewModels;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class CreatePlanModal : ComponentBase
{
    [Inject] public IPlanService PlanService { get; set; } = default!;

    // керування модалкою
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    // повідомити батька про створення (з поверненням створеного плану)
    [Parameter] public EventCallback<PlanViewModel> OnCreated { get; set; }

    private CreatePlanViewModel _model = new(); // мутабельна VM
    private bool _busy;

    public void Open()
    {
        _model = new(); // reset
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task CreateAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var created = await PlanService.CreateAsync(_model);
            await OnCreated.InvokeAsync(created);
            await Close();
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task Close()
    {
        if (OnClose.HasDelegate)
            await OnClose.InvokeAsync();
    }
}