//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ChangePositionDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Persons.Cards.Drawers;

public partial class ChangePositionDrawer
{
    // =========================
    // Parameters
    // =========================  
    [Parameter, EditorRequired] public PersonDetailsDto Person { get; set; } = default!;
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }
    [Parameter] public EventCallback<ChangePositionDto> OnChangePosition { get; set; }

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

    protected ChangePositionDto Model { get; set; } = new();

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

            if (OnChangePosition.HasDelegate)
                await OnChangePosition.InvokeAsync(Model);

            Toasts.Success("Посада змінена");
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

        Model = new ChangePositionDto
        {
            PersonId = Person.Id,                               // ✅ критично
            EffectiveDate = DateOnly.FromDateTime(DateTime.Now),// ✅ дефолт
            PositionSort = Person.PositionSort,               // (можеш лишити null, якщо хочеш примусово обирати)
            Position = Person.Position ?? string.Empty,                 // (можеш лишити пусто, якщо хочеш примусово обирати)
            Note = null
        };

        _editContext = new EditContext(Model);
    }

    private void NormalizeModel()
    {
        Model.Position = TrimOrEmpty(Model.Position);
        Model.Note = string.IsNullOrWhiteSpace(Model.Note) ? null : Model.Note.Trim();
    }

    // =========================
    // Helpers
    // =========================

    private static string TrimOrEmpty(string? s) => (s ?? string.Empty).Trim();
}
