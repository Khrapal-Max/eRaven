//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ErrorBoundaryHub
//-----------------------------------------------------------------------------

namespace eRaven.Presentation.Errors;

public sealed class ErrorBoundaryHub
{
    public event Action? RecoverRequested;

    public void RequestRecover() => RecoverRequested?.Invoke();
}
