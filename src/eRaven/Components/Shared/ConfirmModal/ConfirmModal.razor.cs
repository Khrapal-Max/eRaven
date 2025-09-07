//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ConfirmModal
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.ConfirmModal;

public partial class ConfirmModal : ComponentBase
{
    [Parameter] public string Title { get; set; } = "Підтвердження";
    [Parameter] public string ConfirmText { get; set; } = "Підтвердити";
    [Parameter] public string CancelText { get; set; } = "Скасувати";
    [Parameter] public string DialogSize { get; set; } = "modal-sm"; // modal-sm / modal-lg / modal-xl

    /// <summary>
    /// Кастомний футер із доступом до інстансу модалки.
    /// Приклад: <ConfirmModal><FooterTemplate Context="m"><button @onclick="m.Confirm">Ок</button></FooterTemplate></ConfirmModal>
    /// </summary>
    [Parameter] public RenderFragment<ConfirmModal>? FooterTemplate { get; set; }

    private TaskCompletionSource<object?>? _tcs;

    protected bool Visible { get; set; }
    protected string? BodyText { get; set; }
    private RenderFragment? _customBody;

    // Тримач моделі для формового режиму
    private object? _formModel;

    /// <summary>Просте підтвердження з текстом (true/false).</summary>
    public Task<bool> ShowConfirmAsync(string? bodyText)
    {
        EnsureNotBusy();

        BodyText = bodyText;
        _customBody = null;
        _formModel = null;

        Visible = true;
        _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = InvokeAsync(StateHasChanged);

        return AwaitBoolAsync();
    }

    /// <summary>
    /// Формовий режим: показати модал з вашим RenderFragment на основі моделі.
    /// Повертає підтверджену модель або null, якщо скасовано.
    /// </summary>
    public async Task<TModel?> ShowFormAsync<TModel>(TModel model, RenderFragment<TModel> bodyTemplate)
        where TModel : class
    {
        EnsureNotBusy();

        BodyText = null;
        _formModel = model;                // ← зберігаємо модель
        _customBody = bodyTemplate(model); // ← рендеримо шаблон з контекстом model

        Visible = true;
        _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = InvokeAsync(StateHasChanged);

        var result = await _tcs.Task.ConfigureAwait(false);
        return result as TModel;
    }

    /// <summary>Підтвердити: повертає або true (confirm-mode), або модель (form-mode).</summary>
    public void Confirm()
    {
        if (_tcs is null) return;

        Visible = false;
        var result = _customBody is not null ? _formModel : true;
        _tcs.TrySetResult(result);
        Cleanup();
    }

    /// <summary>Скасувати: повертає або false (confirm-mode), або null (form-mode).</summary>
    public void Cancel()
    {
        if (_tcs is null) return;

        Visible = false;
        var result = _customBody is not null ? null : (object?)false;
        _tcs.TrySetResult(result);
        Cleanup();
    }

    // --- Helpers ---

    private async Task<bool> AwaitBoolAsync()
    {
        var obj = await _tcs!.Task.ConfigureAwait(false);
        return obj is bool b && b;
    }

    private void EnsureNotBusy()
    {
        if (_tcs is not null)
            throw new InvalidOperationException("Модальне вікно вже відкрите.");
    }

    private void Cleanup()
    {
        _customBody = null;
        _formModel = null;

        var tcs = _tcs;
        _tcs = null;

        _ = InvokeAsync(StateHasChanged);
    }
}