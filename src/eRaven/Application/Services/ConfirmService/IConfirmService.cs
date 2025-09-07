//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IConfirmService
//-----------------------------------------------------------------------------

namespace eRaven.Application.Services.ConfirmService;

public interface IConfirmService
{
    Task<bool> AskAsync(string text);
}
