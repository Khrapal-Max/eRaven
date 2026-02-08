//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DocumentEditor
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.DTOs.CombatTask.Models;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class DocumentEditor
{
    //======================================================================
    // DI
    //======================================================================
    [Inject] public IQueryHandler<GetCombatTaskDetailsByDocumentIdQuery, CombatTaskEditorDto?> GetCombatTaskHandler { get; set; } = default!;
    [Inject] public ICommandHandler<CreateCombatTaskCommand, Guid> CreateCombatTaskHandler { get; set; } = default!;
    [Inject] public NavigationManager Nav { get; set; } = default!;
    [Inject] public ToastService Toasts { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================
    [Parameter] public Guid DocumentId { get; set; }

    //======================================================================
    // Drawer state
    //======================================================================

    private bool _createOpen;
    private bool _closeOpen;

    //======================================================================
    // UI state
    //======================================================================

    private bool _loading;
    private bool IsDraft => (_combatTaskDocument?.Status) != DocumentStatus.Draft;
    private CombatTaskEditorDto? _combatTaskDocument;
    private IReadOnlyCollection<CombatTaskMissionBlockDto> _missions = [];

    protected override async Task OnParametersSetAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;

        try
        {
            _combatTaskDocument = await GetCombatTaskHandler.HandleAsync(
                new GetCombatTaskDetailsByDocumentIdQuery(DocumentId));

            _missions = _combatTaskDocument?.Missions ?? [];

            if (_combatTaskDocument is null)
                Toasts.Warning("Документ не знайдено або недоступний.");
        }
        finally
        {
            _loading = false;
        }
    }

    //======================================================================
    // Commands
    //======================================================================

    private async Task HandleCreatedAsync(CreateCombatTaskModel task)
    {
        try
        {
            var command = new CreateCombatTaskCommand(
            DocumentId: DocumentId,
            MissionId: task.MissionId,
            SourceDocument: task.SourceDocument,
            CombatTaskDetails: [.. task.Details.Select(x => new CombatTaskDetailsDto(
                CombatTaskDetailsId: Guid.NewGuid(),
                Kind: x.CombatTaskDetailsKind,
                EffectiveAt: x.EffectiveAt,
                PersonId: x.PersonId,
                Rnokpp: x.Rnokpp,
                FullName: x.FullName,
                Callsign: x.Callsign
            ))]);

            await CreateCombatTaskHandler.HandleAsync(command);
            Toasts.Success($"Призначення на завдання '{task.SourceDocument}' успішно виконано.");
        }
        catch (Exception ex)
        {
            Toasts.Error($"Помилка при призначенні на завдання: {ex.Message}");
            return;
        }
        finally
        {
            _createOpen = false;
        }

        await LoadAsync();
    }

    //======================================================================
    // Actions
    //======================================================================
    private Task OpenCreateDrawer()
    {
        _createOpen = true;
        return Task.CompletedTask;
    }

    private Task OpenCloseDrawer()
    {
        _closeOpen = true;
        return Task.CompletedTask;
    }

    //======================================================================
    // Helpers
    //======================================================================

    private static string KindLabel(CombatTaskDetailsKind kind)
        => kind switch
        {
            CombatTaskDetailsKind.Start => "Почав виконання",
            CombatTaskDetailsKind.End => "Припинив виконання",
            _ => kind.ToString()
        };
}
