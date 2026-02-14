//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseTimesheetCodeDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheet;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Components.Shared.Drawer;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.Timesheet.Drawers;

public partial class CloseTimesheetCodeDrawer : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================

    [Inject] public ICommandHandler<CloseTimesheetCodeCommand> CloseCode { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Params
    //======================================================================

    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>Код, який закриваємо (передається з Policy сторінки).</summary>
    [Parameter] public TimesheetCodeDto? Code { get; set; }

    /// <summary>Сигнал батьку, що код закрито (щоб перезавантажити список).</summary>
    [Parameter] public EventCallback<Guid> OnClosedCode { get; set; }

    //======================================================================
    // State
    //======================================================================

    private Drawer? _drawer;
    private bool _busy;

    //======================================================================
    // Actions
    //======================================================================

    private async Task ConfirmCloseAsync()
    {
        if (_busy) return;
        if (Code is null) return;

        _busy = true;

        try
        {
            await CloseCode.HandleAsync(new CloseTimesheetCodeCommand(
                CodeId: Code.Id,
                Author: "ui",           // TODO: реальний юзер
                NowUtc: DateTime.UtcNow
            ));

            Toasts.Success("Збережено", $"Код {Code.Code} закрито.");

            if (OnClosedCode.HasDelegate)
                await OnClosedCode.InvokeAsync(Code.Id);

            if (_drawer is not null)
                await _drawer.CloseAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error("Не вдалося закрити код", ex.Message);
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
}
