/*//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateDocumentDraftDrawer
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class DocumentEditor
{
    //======================================================================
    // DI: query/command handlers
    //======================================================================

    [Inject] public IQueryHandler<GetCombatTaskDocumentByIdQuery, CombatTaskDocumentDetailsDto?> GetById { get; set; } = default!;

    [Inject] public ICommandHandler<StartCombatTaskGroupCommand, Guid> StartGroup { get; set; } = default!;
    [Inject] public ICommandHandler<EndCombatTaskMissionCommand, Guid> EndMission { get; set; } = default!;
    [Inject] public ICommandHandler<DeleteCombatTaskGroupCommand> DeleteGroup { get; set; } = default!;

    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================

    [Parameter] public Guid DocumentId { get; set; }

    //======================================================================
    // UI state
    //======================================================================

    private bool _loading;
    private CombatTaskDocumentDetailsDto? _doc;
    private IReadOnlyList<CombatEntryDetailsDto> _entries = [];

    private bool _createOpen;

    private bool _endOpen;
    private CombatEntryDetailsDto? _endTarget;

    private bool IsDraft => _doc?.Status == DocumentStatus.Draft;

    protected override async Task OnInitializedAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _doc = await GetById.HandleAsync(new GetCombatTaskDocumentByIdQuery(DocumentId));
            _entries = _doc?.Entries ?? [];
        }
        finally
        {
            _loading = false;
        }
    }

    private Task OpenCreateDrawer()
    {
        _createOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenEndDrawer(CombatEntryDetailsDto g)
    {
        _endTarget = g;
        _endOpen = true;
        return Task.CompletedTask;
    }

    private async Task HandleCreatedAsync(StartCombatTaskGroupModel model)
    {
        _loading = true;

        try
        {
            // TODO: author/nowUtc підстав з auth context
            var author = "system";
            var nowUtc = DateTime.UtcNow;

            await StartGroup.HandleAsync(new StartCombatTaskGroupCommand(
                DocumentId: DocumentId,
                SourceDocNo: model.SourceDocNo,
                MissionId: model.MissionId,
                MissionDisplaySnapshot: model.MissionDisplaySnapshot,
                From: model.From,
                Persons: model.Persons,
                Author: author,
                NowUtc: nowUtc));

            Toasts.Success("Групу участей створено.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleEndedAsync(EndCombatMissionModel model)
    {
        _loading = true;
        try
        {
            var author = "system";
            var nowUtc = DateTime.UtcNow;

            await EndMission.HandleAsync(new EndCombatTaskMissionCommand(
                DocumentId: DocumentId,
                MissionId: model.MissionId,
                PersonIds: model.PersonIds,
                To: model.To,
                EndSourceDocNo: model.EndSourceDocNo,
                Author: author,
                NowUtc: nowUtc));

            Toasts.Success("Участь завершено.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task DeleteGroupAsync(Guid groupId)
    {
        _loading = true;

        try
        {
            var author = "system"; //TODO auth user
            var nowUtc = DateTime.UtcNow;

            await DeleteGroup.HandleAsync(new DeleteCombatTaskGroupCommand(
                DocumentId: DocumentId,
                GroupId: groupId,
                Author: author,
                NowUtc: nowUtc));

            Toasts.Success("Групу видалено.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Toasts.Error(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    private static string IntervalLabel(DateOnly from, DateOnly? to)
        => to is null
            ? $"{from:dd.MM.yyyy} → …"
            : $"{from:dd.MM.yyyy} → {to.Value:dd.MM.yyyy}";
}*/