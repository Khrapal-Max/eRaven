using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Plans.Modals;

public partial class CreatePlanModal : ComponentBase
{
    [Parameter] public bool Open { get; set; }
    [Parameter] public bool Busy { get; set; } = false;

    [Parameter] public EventCallback OnCancel { get; set; }
    [Parameter] public EventCallback<string> OnCreate { get; set; }

    private string? _number;
    private string? _error;

    private bool IsValid
    {
        get
        {
            _error = null;
            var s = (_number ?? string.Empty).Trim();
            if (s.Length == 0) { _error = "Номер плану обов’язковий."; return false; }
            if (s.Length > 64) { _error = "Номер плану не повинен перевищувати 64 символи."; return false; }
            return true;
        }
    }

    private Task Cancel() => OnCancel.InvokeAsync();

    private async Task Create()
    {
        if (!IsValid) return;
        await OnCreate.InvokeAsync((_number ?? string.Empty).Trim());
    }
}
