//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// RegistryToolbar
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Persons.Registry;

public partial class RegistryToolbar
{
    [Parameter] public EventCallback OnCreateReserved { get; set; }
    private Task HandleCreateClick() => OnCreateReserved.InvokeAsync();
}
