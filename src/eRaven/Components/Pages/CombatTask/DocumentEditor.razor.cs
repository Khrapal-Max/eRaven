//-----------------------------------------------------------------------------
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
    // query/command handlers
    [Inject] public IQueryHandler<GetCombatTaskDocumentByIdQuery, CombatTaskDocumentDetailsDto?> GetById { get; set; } = default!;
    [Inject] public ICommandHandler<AddCombatTaskGroupCommand, Guid> AddGroup { get; set; } = default!;
    [Inject] public ICommandHandler<DeleteCombatTaskGroupCommand> DeleteGroup { get; set; } = default!;
    [Inject] public ICommandHandler<UpdateCombatTaskGroupCommand> UpdateGroup { get; set; } = default!;
    [Inject] public ICommandHandler<ReplaceCombatTaskGroupPersonsCommand> ReplacePersons { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

    [Parameter] public Guid DocumentId { get; set; }

    // state для drawer-ів
    private bool _editOpen;
    private bool _personsOpen;

    private Guid _activeGroupId;
    private CreateCombatGroupModel? _editInitial;
    private IReadOnlyList<Guid> _personsInitial = [];

    private bool _loading;
    private bool _createOpen;

    private CombatTaskDocumentDetailsDto? _doc;
    private IReadOnlyList<CombatEntryDetailsDto> _entries = [];

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

    // Drawer повертає CreateCombatGroupModel
    private async Task HandleCreatedAsync(CreateCombatGroupModel model)
    {
        _loading = true;
        try
        {
            // TODO author/nowUtc підстав свої (з auth context)
            var author = "system";
            var nowUtc = DateTime.UtcNow;

            await AddGroup.HandleAsync(new AddCombatTaskGroupCommand(
                DocumentId: DocumentId,
                SourceDocNo: model.SourceDocNo,
                Action: model.Action,
                MissionId: model.MissionId,
                MissionDisplaySnapshot: model.MissionDisplaySnapshot,
                ActionDate: model.ActionDate,
                PersonIds: model.PersonIds,
                Author: author,
                NowUtc: nowUtc
            ));

            ToastService.Success("Місія додана.");

            await LoadAsync();
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandleUpdatedAsync(UpdateCombatGroupModel model)
    {
        _loading = true;
        try
        {
            var author = "system";
            var nowUtc = DateTime.UtcNow;

            await UpdateGroup.HandleAsync(new UpdateCombatTaskGroupCommand(
                DocumentId: DocumentId,
                GroupId: model.GroupId,
                SourceDocNo: model.SourceDocNo,
                Action: model.Action,
                MissionId: model.MissionId,
                MissionDisplaySnapshot: model.MissionDisplaySnapshot,
                ActionDate: model.ActionDate,
                Author: author,
                NowUtc: nowUtc
            ));

            ToastService.Success("Місію оновлено.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
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
            await DeleteGroup.HandleAsync(new DeleteCombatTaskGroupCommand(DocumentId, groupId));

            ToastService.Success("Місія відалена.");

            await LoadAsync();
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    // Викликається після person picker drawer (він повертає список personIds)
    private async Task ReplaceGroupPersonsAsync(Guid groupId, IReadOnlyList<Guid> personIds)
    {
        _loading = true;
        try
        {
            // TODO author/nowUtc підстав свої (з auth context)
            var author = "system";
            var nowUtc = DateTime.UtcNow;

            await ReplacePersons.HandleAsync(new ReplaceCombatTaskGroupPersonsCommand(
                DocumentId: DocumentId,
                GroupId: groupId,
                PersonIds: personIds,
                Author: author,
                NowUtc: nowUtc
            ));

            ToastService.Success("Місія оновлена.");

            await LoadAsync();
        }
        catch (Exception ex)
        {
            ToastService.Error(ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task HandlePersonsSavedAsync(ReplaceGroupPersonsModel model)
    {
        await ReplaceGroupPersonsAsync(model.GroupId, model.PersonIds);
    }

    private Task OpenEditGroupDrawer(CombatEntryDetailsDto g)
    {
        _activeGroupId = g.GroupId;

        // prefill (для drawer)
        _editInitial = new CreateCombatGroupModel(
            SourceDocNo: g.SourceDocNo,
            Action: g.Action,
            MissionId: g.MissionId,
            MissionDisplaySnapshot: g.MissionDisplay,
            ActionDate: g.ActionDate,
            PersonIds: []); // людей редагуємо окремим drawer

        _editOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenPersonsDrawer(CombatEntryDetailsDto g)
    {
        _activeGroupId = g.GroupId;
        _personsInitial = [.. g.Persons.Select(p => p.PersonId).Distinct()];
        _personsOpen = true;
        return Task.CompletedTask;
    }

    private static string ActionKindLabel(ActionKind action)
         => action switch
         {
             ActionKind.Start => "Почати виконання",
             ActionKind.End => "Закінчити виконання",
             _ => action.ToString()
         };
}