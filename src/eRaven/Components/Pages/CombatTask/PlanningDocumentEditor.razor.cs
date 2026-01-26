//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PlanningDocumentEditor
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class PlanningDocumentEditor
{
    [Parameter] public Guid DocumentId { get; set; }

    [Inject] public NavigationManager Nav { get; set; } = default!;

    private bool _loading;
    private string? _error;

    // header fields (поки без DTO)
    private DateOnly? _recordedAt;
    private DateOnly? _planningDate;
    private string? _title;
    private CombatTaskPlanDocumentStatus? _status;
    private string? _statusText;
    private bool IsDraft => _status == CombatTaskPlanDocumentStatus.Draft;

    // lines skeleton model (тимчасово, поки не заведеш DTO/Query)
    private readonly List<LineRow> _lines = [];

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;

        try
        {
            // TODO: підʼєднати Query:
            // - GetPlanningDocumentByIdQuery (header + lines)
            // Поки — заглушка:
            _recordedAt = null;
            _planningDate = null;
            _title = null;
            _status = CombatTaskPlanDocumentStatus.Draft;
            _statusText = "Чернетка";
            _lines.Clear();
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private void GoAddLine()
        => Nav.NavigateTo($"/planning-documents/{DocumentId}/line/new");

    private void GoEditLine(Guid lineId)
        => Nav.NavigateTo($"/planning-documents/{DocumentId}/line/{lineId}");

    private Task DeleteLine(Guid lineId)
    {
        // TODO: Draft-only команда DeleteLine
        _lines.RemoveAll(x => x.LineId == lineId);
        return Task.CompletedTask;
    }

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

    private sealed class LineRow
    {
        public Guid LineId { get; init; }
        public string Kind { get; init; } = "";
        public DateOnly ActionDate { get; init; }
        public string FullName { get; init; } = "";
        public string PositionalArea { get; init; } = "";
        public string GroupName { get; init; } = "";
        public string Mode { get; init; } = "";
        public string Goal { get; init; } = "";
    }
}
