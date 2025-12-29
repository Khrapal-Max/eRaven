//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ModalComponent
//-----------------------------------------------------------------------------

using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Shared.ModalComponent;

public partial class ModalComponent : ComponentBase
{
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    [Parameter] public string Title { get; set; } = "Створення";
    [Parameter] public string CreateText { get; set; } = "Створити";
    [Parameter] public string CancelText { get; set; } = "Скасувати";
    [Parameter] public string DialogSize { get; set; } = "modal-lg";

    [Parameter] public bool IsBusy { get; set; }

    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>
    /// Return true => close modal, false => keep open (validation/server error).
    /// </summary>
    [Parameter] public Func<Task<bool>>? OnCreateAsync { get; set; }

    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task Cancel()
    {
        if (IsBusy) return;

        if (OnCancel.HasDelegate)
            await OnCancel.InvokeAsync();

        await SetOpen(false);
    }

    private async Task Create()
    {
        if (IsBusy) return;

        var canClose = true;
        if (OnCreateAsync is not null)
            canClose = await OnCreateAsync();

        if (canClose)
            await SetOpen(false);
    }

    private async Task SetOpen(bool value)
    {
        IsOpen = value;
        if (IsOpenChanged.HasDelegate)
            await IsOpenChanged.InvokeAsync(value);
    }
}
