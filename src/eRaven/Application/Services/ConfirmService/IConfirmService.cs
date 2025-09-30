//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IConfirmService
//-----------------------------------------------------------------------------

using System;
using System.Threading;

namespace eRaven.Application.Services.ConfirmService;

public interface IConfirmService
{
    /// <summary>
    /// Взаємодія з користувачем для отримання підтвердження (так/ні).
    /// </summary>
    /// <param name="text"></param>
    /// <returns>bool</returns>
    Task<bool> AskAsync(string text, CancellationToken cancellationToken = default, TimeSpan? timeout = null);

    /// <summary>
    /// Рєєстрація провайдера підтверджень.
    /// </summary>
    /// <param name="provider"></param>
    void Use(Func<string, Task<bool>> provider);
}
