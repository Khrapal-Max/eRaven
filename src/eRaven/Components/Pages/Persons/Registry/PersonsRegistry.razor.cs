//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonsRegistry
//-----------------------------------------------------------------------------

namespace eRaven.Components.Pages.Persons.Registry;

public partial class PersonsRegistry
{
    private bool _drawerOpen;

    private Task OpenCreateReserved()
    {
        _drawerOpen = true;
        return Task.CompletedTask;
    }

    private Task CloseDrawer()
    {
        _drawerOpen = false;
        return Task.CompletedTask;
    }

    private Task OnDrawerClosed()
    {
        // опційно: очистити state/validation
        return Task.CompletedTask;
    }
}