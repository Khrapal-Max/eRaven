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
    private readonly IJSRuntime _js = js;
    private Func<string, Task<bool>>? _provider;

    public void Use(Func<string, Task<bool>> provider) => _provider = provider;

    public Task<bool> AskAsync(string text)
        => _provider is not null
           ? _provider.Invoke(text)
           : _js.InvokeAsync<bool>("confirm", text).AsTask();
}