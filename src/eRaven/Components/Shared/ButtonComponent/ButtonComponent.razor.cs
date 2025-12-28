//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ButtonComponent
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.ButtonComponent;

public partial class ButtonComponent : ComponentBase
{
    [Parameter]
    public string? Label { get; set; }

    [Parameter]
    public string Class { get; set; } = string.Empty;

    [Parameter]
    public string Style { get; set; } = string.Empty;

    [Parameter]
    public EventCallback OnClickButton { get; set; }

    [Parameter]
    public bool IsDisabled { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    public Task ClickButton() => OnClickButton.InvokeAsync();
}
