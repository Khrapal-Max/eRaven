//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// JsRuntimeInvokeExtensions
//-----------------------------------------------------------------------------

using Microsoft.JSInterop;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace eRaven.Application.Services.JsInterop;

public static class JsRuntimeInvokeExtensions
{
    public static async ValueTask<T> InvokeWithCancellationAsync<T>(
        this IJSRuntime jsRuntime,
        string identifier,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null,
        params object?[] args)
    {
        if (!timeout.HasValue && !cancellationToken.CanBeCanceled)
        {
            return await jsRuntime.InvokeAsync<T>(identifier, args).ConfigureAwait(false);
        }

        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource? linkedCts = null;
        var token = cancellationToken;

        if (timeout.HasValue)
        {
            timeoutCts = new CancellationTokenSource(timeout.Value);
            token = cancellationToken.CanBeCanceled
                ? (linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)).Token
                : timeoutCts.Token;
        }

        try
        {
            return await jsRuntime.InvokeAsync<T>(identifier, token, args).ConfigureAwait(false);
        }
        finally
        {
            linkedCts?.Dispose();
            timeoutCts?.Dispose();
        }
    }

    public static async ValueTask InvokeVoidWithCancellationAsync(
        this IJSRuntime jsRuntime,
        string identifier,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null,
        params object?[] args)
    {
        if (!timeout.HasValue && !cancellationToken.CanBeCanceled)
        {
            await jsRuntime.InvokeVoidAsync(identifier, args).ConfigureAwait(false);
            return;
        }

        CancellationTokenSource? timeoutCts = null;
        CancellationTokenSource? linkedCts = null;
        var token = cancellationToken;

        if (timeout.HasValue)
        {
            timeoutCts = new CancellationTokenSource(timeout.Value);
            token = cancellationToken.CanBeCanceled
                ? (linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)).Token
                : timeoutCts.Token;
        }

        try
        {
            await jsRuntime.InvokeVoidAsync(identifier, token, args).ConfigureAwait(false);
        }
        finally
        {
            linkedCts?.Dispose();
            timeoutCts?.Dispose();
        }
    }
}
