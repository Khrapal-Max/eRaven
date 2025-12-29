//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ConfirmModal
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.ConfirmModalComponent;

public partial class ConfirmModalComponent<T> : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Підтвердження";
    [Parameter] public string ConfirmText { get; set; } = "Закрити";
    [Parameter] public string CancelText { get; set; } = "Скасувати";
    [Parameter] public string DialogSize { get; set; } = "modal-sm";

    // Якщо хочеш кастомний контент (наприклад показати назву посади):
    [Parameter] public RenderFragment<T>? BodyTemplate { get; set; }

    protected bool Visible { get; set; }

    private TaskCompletionSource<bool>? _tcs;
    private T? _model;
    protected string? BodyText { get; set; }

    /// <summary>
    /// Показати підтвердження для моделі. Повертає true, якщо підтверджено.
    /// </summary>
    public async Task<bool> ShowAsync(T model, string? bodyText = null)
    {
        EnsureNotBusy();

        _model = model;
        BodyText = bodyText;

        Visible = true;
        _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        await InvokeAsync(StateHasChanged);

        return await _tcs.Task;
    }

    public async Task Confirm() => await Close(true);
    public async Task Cancel() => await Close(false);

    private async Task Close(bool result)
    {
        if (_tcs is null) return;

        Visible = false;
        _tcs.TrySetResult(result);

        _tcs = null;
        _model = default;
        BodyText = null;

        await InvokeAsync(StateHasChanged);
    }

    private void EnsureNotBusy()
    {
        if (_tcs is not null)
            throw new InvalidOperationException("Модальне вікно вже відкрите.");
    }
}
