//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionCloseDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Mission;
using eRaven.Application.DTOs.Mission;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Mission.Drawers;

public partial class MissionCloseDrawer
{
    //=======================
    // DI
    //=======================
    [Inject] public ICommandHandler<CloseMissionCommand> CloseMission { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>
    /// Місія, яку закриваємо (передається із таблиці).
    /// </summary>
    [Parameter] public MissionDto? Mission { get; set; }

    /// <summary>
    /// Викликається після успішного закриття (щоб shell зробив ReloadAsync()).
    /// </summary>
    [Parameter] public EventCallback OnClosed { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    //=======================
    // UI
    //=======================
    private bool _busy;

    private EditContext _editContext = default!;
    protected CloseModelDto Model { get; set; } = new();

    private bool DisabledSave =>
        _busy || Mission is null;

    //=======================
    // Lifecycle
    //=======================
    protected override void OnInitialized() => Reset();

    protected override Task OnParametersSetAsync()
    {
        if (IsOpen)
        {
            // при відкритті — ставимо дефолт: сьогодні (або CreatedAt, якщо треба)
            Reset();
        }
        return Task.CompletedTask;
    }

    //=======================
    // Commands
    //=======================
    private void Reset()
    {
        _busy = false;

        Model = new CloseModelDto
        {
            ClosedAt = DateOnly.FromDateTime(DateTime.Today)
        };

        _editContext = new EditContext(Model);
    }

    private async Task SubmitAsync()
    {
        if (_busy || Mission is null) return;

        _busy = true;

        try
        {
            await CloseMission.HandleAsync(new CloseMissionCommand(
                MissionId: Mission.MissionId,
                ClosedAt: Model.ClosedAt));

            Toasts.Error("Місія язакрита.");

            if (OnClosed.HasDelegate)
                await OnClosed.InvokeAsync();

            await IsOpenChanged.InvokeAsync(false);
        }
        catch (Exception ex)
        {
            Reset();
            Toasts.Error(ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    //=======================
    // Actions
    //=======================
    private async Task OnCancel()
    {
        if (_busy) return;
        await IsOpenChanged.InvokeAsync(false);
    }

    private Task OnDrawerClosed()
    {
        Reset();
        return Task.CompletedTask;
    }
}
