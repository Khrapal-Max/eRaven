//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// VoidPersonalEventDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Card.Drawers;

public partial class VoidPersonalEventDrawer
{
    // =========================
    // Parameters
    // =========================
    [Parameter, EditorRequired] public Guid PersonId { get; set; }
    [Parameter] public Guid? TargetEventId { get; set; }
    [Parameter] public string? TargetTitle { get; set; }
    [Parameter] public string? TargetDetails { get; set; }

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<VoidPersonEventDto> OnVoidEvent { get; set; }

    // =========================
    // DI
    // =========================
    [Inject] public ToastService Toasts { get; set; } = default!;

    // =========================
    // State
    // =========================
    private bool _busy;
    private bool _wasOpen;
    private EditContext _editContext = default!;

    protected VoidPersonEventDto Model { get; set; } = new();

    private bool IsSubmitDisabled => _busy || TargetEventId is null;
    private string TargetEventIdText => TargetEventId?.ToString() ?? "— не обрано —";

    // =========================
    // Lifecycle
    // =========================
    protected override void OnInitialized()
    {
        ResetModel();
    }

    protected override Task OnParametersSetAsync()
    {
        // Open transition
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            ResetModel();
            return Task.CompletedTask;
        }

        // While open: keep TargetEventId in sync if parent changes it
        if (IsOpen && _wasOpen)
        {
            var newTarget = TargetEventId ?? Guid.Empty;
            if (Model.TargetEventId != newTarget)
            {
                Model.TargetEventId = newTarget;
                _editContext.NotifyFieldChanged(new FieldIdentifier(Model, nameof(Model.TargetEventId)));
            }

            // keep person id in sync too
            if (Model.PersonId != PersonId)
                Model.PersonId = PersonId;
        }

        // Close transition (state cleanup happens in OnDrawerClosed)
        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
        }

        return Task.CompletedTask;
    }

    // =========================
    // UI actions
    // =========================
    private async Task OnVoidAsync()
    {
        if (_busy) return;

        _busy = true;
        try
        {
            Normalize();

            if (OnVoidEvent.HasDelegate)
                await OnVoidEvent.InvokeAsync(Model);

            Toasts.Success("Подію відмінено");
            await IsOpenChanged.InvokeAsync(false);
        }
        catch (InvalidOperationException ex)
        {
            Toasts.Warning("Неможливо виконати дію", ex.Message);
        }
        catch
        {
            Toasts.Error("Помилка", "Сталася неочікувана помилка. Спробуйте ще раз.");
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task OnCancel()
    {
        if (_busy) return;
        await IsOpenChanged.InvokeAsync(false);
    }

    private Task OnDrawerClosed()
    {
        ResetModel();
        return Task.CompletedTask;
    }

    // =========================
    // Internals
    // =========================
    private void ResetModel()
    {
        _busy = false;

        Model = new VoidPersonEventDto
        {
            PersonId = PersonId,                          // ✅ важливо
            TargetEventId = TargetEventId ?? Guid.Empty,  // ✅ важливо
            Reason = string.Empty
        };

        _editContext = new EditContext(Model);
    }

    private void Normalize() => Model.Reason = (Model.Reason ?? string.Empty).Trim();
}