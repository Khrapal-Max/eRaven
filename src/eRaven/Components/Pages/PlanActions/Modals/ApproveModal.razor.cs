//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ApproveModal
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using eRaven.Application.ViewModels.PlanActionViewModels;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PlanActions.Modals;

public partial class ApproveModal : ComponentBase
{
    [Parameter] public EventCallback<ApprovePlanActionViewModel> OnApproved { get; set; }

    private bool IsOpen { get; set; }
    private bool _busy;
    private FluentValidationValidator? _validator;

    // Модель для EditForm — НЕ null
    private ApprovePlanActionViewModel ViewModel { get; set; } = new();

    /// <summary>Відкрити модалку. Перед цим обовʼязково передай PlanAction або через параметр, або сюди.</summary>
    public void Open(PlanAction planAction)
    {
        if (planAction is null) return; // або кинути InvalidOperationException

        ViewModel = new ApprovePlanActionViewModel
        {
            Id = planAction.Id,
            PersonId = planAction.PersonId,
            EffectiveAtUtc = planAction.EffectiveAtUtc,
            Order = string.Empty
        };

        _busy = false;
        IsOpen = true;
        StateHasChanged();
    }

    private void OnOrderChanged(ChangeEventArgs e)
    {
        ViewModel.Order = Convert.ToString(e.Value) ?? string.Empty;
    }

    private async Task ApproveAction()
    {
        /* if (await _validator!.ValidateAsync()) return;*/

        try
        {
            _busy = true;

            ViewModel.Order = (ViewModel.Order ?? string.Empty).Trim();

            await OnApproved.InvokeAsync(ViewModel);
            Close();
        }
        finally
        {
            _busy = false;
        }
    }

    private void Close() => IsOpen = false;
}
