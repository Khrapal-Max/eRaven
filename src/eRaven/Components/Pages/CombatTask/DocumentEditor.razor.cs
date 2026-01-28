//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateDocumentDraftDrawer
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class DocumentEditor
{
    [Inject] public NavigationManager Nav { get; set; } = default!;

    [Parameter] public Guid DocumentId { get; set; }

    private bool _loading;
    private bool _createOpen;

    private string? _title;
    private DateOnly? _recordedAt;
    private string? _statusText;
    private DocumentStatus? _status;

    private bool IsDraft => _status == DocumentStatus.Draft;

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;

        try
        {
            // TODO: підʼєднати Query:
            // - GetPlanningDocumentByIdQuery (header + lines)
            // Поки — заглушка:
            _title = null;
            _recordedAt = null;
            _title = null;
            _status = DocumentStatus.Draft;
            _statusText = "Чернетка";
        }
        catch (Exception ex)
        {

        }
        finally
        {
            _loading = false;
        }
    }

    /*private void GoAddLine()
        => Nav.NavigateTo($"/planning-documents/{DocumentId}/line/new");

    private void GoEditLine(Guid lineId)
        => Nav.NavigateTo($"/planning-documents/{DocumentId}/line/{lineId}");*/

    private Task OpenCreateDrawer()
    {
        _createOpen = true;
        return Task.CompletedTask;
    }

    private async Task HandleCreatedAsync(Guid _)
        => await LoadAsync();

    private Task PostAsync()
    {
        // TODO: команда PostDocument
        return Task.CompletedTask;
    }

    private Task CancelAsync()
    {
        // TODO: команда CancelDocument
        return Task.CompletedTask;
    }
}
