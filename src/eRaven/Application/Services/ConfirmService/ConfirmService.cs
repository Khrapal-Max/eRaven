//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ConfirmService
//-----------------------------------------------------------------------------

using Microsoft.JSInterop;

namespace eRaven.Application.Services.ConfirmService;

public sealed class ConfirmService(IJSRuntime js) : IConfirmService
{
    private Func<string, Task<bool>>? _provider;

    public void RegisterProvider(Func<string, Task<bool>> provider) => _provider = provider;
    public void ResetProvider() => _provider = null;

    public async Task<bool> AskAsync(string text)
    {
        if (_provider is not null) return await _provider.Invoke(text);
        // Фолбек: нативний window.confirm, якщо провайдер ще не зареєстрований
        return await js.InvokeAsync<bool>("confirm", text);
    }
}