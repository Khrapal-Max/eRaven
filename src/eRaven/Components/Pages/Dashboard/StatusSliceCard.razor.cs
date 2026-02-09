//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// StatusSliceCard
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Dashboard;

public partial class StatusSliceCard : ComponentBase
{
    [Parameter] public int Count { get; set; }
    [Parameter] public string Hint { get; set; } = "";
    [Parameter] public string BadgeClass { get; set; } = "text-bg-secondary";
    [Parameter] public EventCallback OnClick { get; set; }

    private async Task HandleClick()
    {
        if (OnClick.HasDelegate)
            await OnClick.InvokeAsync();
    }
}
