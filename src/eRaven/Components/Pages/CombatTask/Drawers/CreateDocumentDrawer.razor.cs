//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateDocumentDraftDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Application.DTOs.CombatTask.Models;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.CombatTask.Drawers;

public partial class CreateDocumentDrawer
{
    //==========================
    // DI
    //==========================
    [Inject] public ICommandHandler<CreateCombatTaskDocumentCommand, Guid> CreateHanler { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;
    [Parameter] public bool IsOpen { get; set; }

    /// <summary>
    /// Повертаємо ID створеної чернетки (щоб shell зробив NavigateTo).
    /// </summary>
    [Parameter] public EventCallback<Guid> OnCreated { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    //==========================
    // UI
    //==========================
    private bool _busy;
    private bool _wasOpen;

    private EditContext _editContext = default!;
    protected CreateCombatTaskDocumentModel Model { get; set; } = new();

    private bool DisabledSave => _busy || string.IsNullOrWhiteSpace(Model.OrderTitle);

    //==========================
    // Lifecycle
    //==========================
    protected override void OnInitialized() => Reset();

    protected override Task OnParametersSetAsync()
    {
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

    //==========================
    // Commands
    //==========================
    private void Reset()
    {
        _busy = false;

        Model = new CreateCombatTaskDocumentModel()
        {
            OrderTitle = string.Empty,
            Description = string.Empty,
            RecordedAt = DateOnly.FromDateTime(DateTime.Today)
        };

        _editContext = new EditContext(Model);
    }

    private async Task SubmitAsync()
    {
        if (_busy) return;

        _busy = true;

        try
        {
            var title = (Model.OrderTitle ?? string.Empty).Trim();
            Model.OrderTitle = title;

            var docId = await CreateHanler.HandleAsync(new CreateCombatTaskDocumentCommand(
                OrderTitle: Model.OrderTitle,
                Description: Model.Description,
                RecordedAt: Model.RecordedAt,
                Author: "ui", // TODO auth user
                NowUtc: DateTime.UtcNow));

            ToastService.Success("Документ створено.");

            if (OnCreated.HasDelegate)
                await OnCreated.InvokeAsync(docId);

            await IsOpenChanged.InvokeAsync(false);
        }
        catch (Exception ex)
        {
            ToastService.Error($"Помилка: {ex.Message}");
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
        Reset();
        return Task.CompletedTask;
    }
}
