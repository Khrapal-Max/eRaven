//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddTimesheetCodeDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Timesheets.Models;
using eRaven.Components.Shared.Drawer;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Timesheets.Drawers;

public partial class AddTimesheetCodeDrawer : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public ICommandHandler<AddTimesheetCodeCommand, Guid> AddCode { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Params
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Повертаємо Id створеного коду (щоб батько оновив список і/або вибрав його).</summary>
    [Parameter] public EventCallback<Guid> OnCreated { get; set; }

    // Опційно: дефолтні значення для зручності
    [Parameter] public int DefaultSortOrder { get; set; } = 0;
    [Parameter] public int DefaultPriority { get; set; } = 0;

    //======================================================================
    // State
    //======================================================================

    private Drawer? _drawer;

    private AddTimesheetCodeModel _model = new();
    private EditContext _editContext = default!;

    private bool _busy;
    private bool _wasOpen;

    //======================================================================
    // Lifecycle
    //======================================================================

    protected override void OnInitialized()
    {
        ResetForm();
    }

    protected override void OnParametersSet()
    {
        // коли відкрили — ініціалізуємо “чисту” форму
        if (IsOpen && !_wasOpen)
            ResetForm();

        _wasOpen = IsOpen;
    }

    //======================================================================
    // Actions
    //======================================================================

    private async Task CreateAsync()
    {
        if (_busy) return;

        // валідація UI
        if (!_editContext.Validate())
            return;

        var code = (_model.Code ?? string.Empty).Trim();
        var title = (_model.Title ?? string.Empty).Trim();
        var desc = string.IsNullOrWhiteSpace(_model.Description) ? null : _model.Description.Trim();

        // NB — системний стан (якщо у вас є глобальна константа — підставте її тут)
        if (string.Equals(code, "НБ", StringComparison.OrdinalIgnoreCase))
        {
            Toasts.Warning("Код “НБ” є системним станом і не може бути створений як подія.");
            return;
        }

        _busy = true;

        try
        {
            var id = await AddCode.HandleAsync(new AddTimesheetCodeCommand(
                Code: code,
                Title: title,
                Description: desc,
                SortOrder: _model.SortOrder,
                Priority: _model.Priority,
                IsTerminal: _model.IsTerminal,
                Author: "ui",           // TODO: підхопити реального юзера
                NowUtc: DateTime.UtcNow // або передати з батька
            ));

            Toasts.Success("Створено", $"Додано код {code}.");

            if (OnCreated.HasDelegate)
                await OnCreated.InvokeAsync(id);

            if (_drawer is not null)
                await _drawer.CloseAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error("Не вдалося створити код", ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private async Task CloseAsync()
    {
        if (_drawer is not null)
            await _drawer.CloseAsync();
    }

    private Task OnClosedAsync()
    {
        ResetForm();
        return Task.CompletedTask;
    }

    //======================================================================
    // Helpers
    //======================================================================

    private void ResetForm()
    {
        _model = new AddTimesheetCodeModel
        {
            Code = string.Empty,
            Title = string.Empty,
            Description = null,
            SortOrder = DefaultSortOrder,
            Priority = DefaultPriority,
            IsTerminal = false
        };

        _editContext = new EditContext(_model);
        _busy = false;
    }
}
