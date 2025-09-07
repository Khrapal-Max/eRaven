//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ConfirmService
//-----------------------------------------------------------------------------

using eRaven.Components.Shared.ConfirmModal;

namespace eRaven.Application.Services.ConfirmService;

public class ConfirmService(ConfirmModal modal) : IConfirmService
{
    private readonly ConfirmModal _modal = modal;

    public Task<bool> AskAsync(string text) =>
        _modal.ShowConfirmAsync(text);
}
