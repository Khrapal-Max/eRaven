/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// ApproveModal
//-----------------------------------------------------------------------------

using Blazored.FluentValidation;
using eRaven.Domain.Models;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.PlanActions.Modals;

public partial class ApproveModal : ComponentBase
{
    [Parameter] public EventCallback<ApprovePlanActionViewModel> OnApproved { get; set; }

    private bool IsOpen { get; set; }
    private bool _busy;
    private FluentValidationValidator? _validator;

    // НЕ null, щоб EditForm мав модель
    private ApprovePlanActionViewModel ViewModel { get; set; } = new();

    /// <summary>Відкрити модалку для конкретної PlanAction.</summary>
    public void Open(PlanAction planAction)
    {
        if (planAction is null) return;

        ViewModel = new ApprovePlanActionViewModel
        {
            Id = planAction.Id,
            PersonId = planAction.PersonId,
            EffectiveAtUtc = planAction.EffectiveAtUtc,
            MoveType = planAction.MoveType,      // <<< додано
            Order = string.Empty
        };

        _busy = false;
        IsOpen = true;
        StateHasChanged();
    }

    private async Task ApproveAction()
    {
        // якщо валідатор підключений — поважаємо його
        if (_validator is not null && !await _validator.ValidateAsync())
            return;

        try
        {
            _busy = true;

            // trim номера наказу
            ViewModel.Order = (ViewModel.Order ?? string.Empty).Trim();

            // кидаємо все наверх — тепер включно з MoveType
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
*/