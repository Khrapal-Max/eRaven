//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionCreateDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.CombatTask.Targets;
using eRaven.Application.Catalogs.CombatTask.TypeDrones;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Mission;
using eRaven.Application.DTOs.Mission;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.Mission.Drawers;

public partial class MissionCreateDrawer : ComponentBase
{
    //=======================
    // DI
    //=======================
    [Inject] public ICommandHandler<CreateMissionCommand, Guid> CreateMissionHandler { get; set; } = default!;
    [Inject] public ITypeDroneCatalog TypeDrone { get; set; } = default!;
    [Inject] public ITargetCatalog TargetCatalog { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Викликається після успішного створення місії.
    /// </summary>
    [Parameter] public EventCallback<Guid> OnCreated { get; set; }

    //=======================
    // UI
    //=======================
    private bool _busy;
    private bool _wasOpen;

    private EditContext _editContext = default!;
    protected CreateMissionDto Model { get; set; } = new();

    private IReadOnlyList<TypeDroneOption> _typeDrones = [];
    private IReadOnlyList<TargetOption> _targetOptions = [];

    private bool DisabledSave =>
        _busy
        || string.IsNullOrWhiteSpace(Model.PositionArea)
        || string.IsNullOrWhiteSpace(Model.Target);

    //=======================
    // Lifecycle
    //=======================
    protected override void OnInitialized() => Reset();

    protected override Task OnParametersSetAsync()
    {
        if (IsOpen)
        {
            _typeDrones = TypeDrone.GetActive();
            _targetOptions = TargetCatalog.GetActive();
        }

        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            Reset();
        }

        if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            Reset();
        }

        return Task.CompletedTask;
    }

    private void Reset()
    {
        _busy = false;

        Model = new CreateMissionDto
        {
            PositionArea = string.Empty,
            NamePoint = null,
            DroneName = null,
            MissionMode = MissionMode.Day,
            Target = string.Empty
        };

        _editContext = new EditContext(Model);
    }

    //=======================
    // Commands
    //=======================
    private async Task SubmitAsync()
    {
        if (_busy) return;

        _busy = true;

        try
        {
            var positionArea = (Model.PositionArea ?? string.Empty).Trim();
            var namePoint = string.IsNullOrWhiteSpace(Model.NamePoint) ? null : Model.NamePoint.Trim();
            var drone = string.IsNullOrWhiteSpace(Model.DroneName) ? null : Model.DroneName.Trim();
            var target = (Model.Target ?? string.Empty).Trim();

            var id = await CreateMissionHandler.HandleAsync(new CreateMissionCommand(
                PositionArea: positionArea,
                NamePoint: namePoint,
                Target: target,
                MissionMode: Model.MissionMode,
                DroneName: drone,
                TodayLocal: DateTime.Now
            ));

            Toasts.Success("Місія створена.");

            if (OnCreated.HasDelegate)
                await OnCreated.InvokeAsync(id);

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

    //=======================
    // Helpers
    //=======================
    private static string GetMissionMode(MissionMode mode)
       => mode switch
       {
           MissionMode.Day => "День",
           MissionMode.Night => "Ніч",
           MissionMode.FullTime => "Цілодобово",
           _ => "Всі види"
       };
}
