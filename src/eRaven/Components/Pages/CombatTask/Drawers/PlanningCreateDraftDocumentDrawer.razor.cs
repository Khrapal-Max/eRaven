//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningCreateDraftDocumentDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Application.DTOs.CombatTask;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace eRaven.Components.Pages.CombatTask.Drawers;

public partial class PlanningCreateDraftDocumentDrawer
{
    [Inject] public ICommandHandler<CreateCombatTaskPlanDraftDocumentCommand, Guid> CreateDraft { get; set; } = default!;
    [Parameter] public bool IsOpen { get; set; }
    [Parameter] public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Повертаємо ID створеної чернетки (щоб shell зробив NavigateTo).
    /// </summary>
    [Parameter] public EventCallback<Guid> OnCreated { get; set; }

    private bool _busy;
    private bool _wasOpen;
    private string? _error;

    private EditContext _editContext = default!;
    protected CreateDraftModelDto Model { get; set; } = new();

    private bool DisabledSave =>
        _busy || string.IsNullOrWhiteSpace(Model.PlanningDocTitle);

    protected override void OnInitialized() => Reset();

    protected override Task OnParametersSetAsync()
    {
        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            Reset(); // на кожне відкриття — чистий стан
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
        _error = null;

        Model = new CreateDraftModelDto
        {
            RecordedAt = DateOnly.FromDateTime(DateTime.Today),
            PlanningDate = DateOnly.FromDateTime(DateTime.Today),
            PlanningDocTitle = string.Empty
        };

        _editContext = new EditContext(Model);
    }

    private async Task SubmitAsync()
    {
        if (_busy) return;

        _busy = true;
        _error = null;

        try
        {
            var title = (Model.PlanningDocTitle ?? string.Empty).Trim();
            Model.PlanningDocTitle = title;

            // Генеруємо ID тут, щоб не вимагати handler з return
            var documentId = Guid.NewGuid();

            await CreateDraft.HandleAsync(new CreateCombatTaskPlanDraftDocumentCommand(
                DocumentId: documentId,
                RecordedAt: Model.RecordedAt,
                PlanningDate: Model.PlanningDate,
                PlanningDocTitle: title,
                Author: "ui",
                NowUtc: DateTime.UtcNow
            ));

            if (OnCreated.HasDelegate)
                await OnCreated.InvokeAsync(documentId);

            await IsOpenChanged.InvokeAsync(false);
        }
        catch (Exception ex)
        {
            _error = ex.Message;
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
