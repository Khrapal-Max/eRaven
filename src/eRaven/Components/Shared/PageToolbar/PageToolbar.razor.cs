//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PageToolbarComponent
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.PageToolbar;

public partial class PageToolbar : ComponentBase
{
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? Left { get; set; }
    [Parameter, EditorRequired] public RenderFragment Right { get; set; } = default!;
}