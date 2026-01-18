//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangeCallsingDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Person;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Cards.Drawers;

public partial class ChangeCallsingDrawer
{
    // =========================
    // Parameters
    // =========================  
    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<ChangeCallsingDto> OnChangeCallsing { get; set; }

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

    protected ChangeCallsingDto Model { get; set; } = new();

    // =========================
    // Lifecycle
    // =========================

    protected override void OnInitialized()
    {
        Reset();
    }

    protected override Task OnParametersSetAsync()
    {
        // Open transition
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            Reset();
            return Task.CompletedTask;
        }

        // Close transition
        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            Reset();
        }

        return Task.CompletedTask;
    }

    // =========================
    // UI actions
    // =========================

    private async Task OnChangeAsync()
    {
        if (_busy)
            return;

        _busy = true;

        try
        {
            NormalizeModel();

            if (OnChangeCallsing.HasDelegate)
                await OnChangeCallsing.InvokeAsync(Model);

            Toasts.Success("Запис змінено");
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
        if (_busy)
            return;

        await IsOpenChanged.InvokeAsync(false);
    }

    private Task OnDrawerClosed()
    {
        Reset();
        return Task.CompletedTask;
    }

    // =========================
    // Internals
    // =========================

    private void Reset()
    {
        if (Person is null) return;

        _busy = false;

        Model = new ChangeCallsingDto
        {
            PersonId = Person.Id,                               // ✅ критично
            EffectiveDate = DateOnly.FromDateTime(DateTime.Now),// ✅ дефолт
            Callsign = Person.Callsign ?? string.Empty
        };

        _editContext = new EditContext(Model);
    }

    private void NormalizeModel()
    {
        Model.Callsign = string.IsNullOrWhiteSpace(Model.Callsign) ? null : Model.Callsign.Trim();
    }
}
