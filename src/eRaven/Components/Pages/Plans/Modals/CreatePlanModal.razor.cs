/*// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// CreatePlanModal
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class CreatePlanModal : ComponentBase
{
    // Вхідні параметри
    [Parameter] public bool Open { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<string> OnCreate { get; set; }

    // Локальний стан
    protected string? _number;
    protected string? _error;

    protected bool IsCreateDisabled
        => Busy || string.IsNullOrWhiteSpace(Trimmed);

    protected string Trimmed
        => (_number ?? string.Empty).Trim();

    protected async Task OnCancelClicked()
    {
        _error = null;
        _number = null;
        await OnCancel.InvokeAsync();
    }

    protected async Task OnCreateClicked()
    {
        _error = null;

        var v = Trimmed;

        // міні-валідатор
        if (v.Length < 2 || v.Length > 64)
        {
            _error = "Довжина назви має бути 2–64 символи.";
            StateHasChanged();
            return;
        }

        await OnCreate.InvokeAsync(v);
    }
}
*/