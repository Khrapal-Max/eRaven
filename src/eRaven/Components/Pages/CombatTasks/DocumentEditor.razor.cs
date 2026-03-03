//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DocumentEditor
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTasks;
using eRaven.Application.DTOs.Enums;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTasks;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTasks;

public partial class DocumentEditor : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================
    [Inject] public IQueryHandler<GetCombatTaskDetailsByDocumentIdQuery, CombatTaskEditorDto> GetCombatTaskDetailsByDocumentIdQueryHandler { get; set; } = default!;
    [Inject] public NavigationManager NavigationManager { get; set; } = default!;
    [Inject] public ToastService ToastService { get; set; } = default!;

    //======================================================================
    // Parameters
    //======================================================================
    [Parameter] public Guid DocumentId { get; set; }

    //======================================================================
    // UI state
    //======================================================================

    private bool _loading;

    private CombatTaskEditorDto? _combatTaskDocument;
    private IReadOnlyCollection<CombatTaskMissionBlockDto> _missions = [];

    //======================================================================
    // Lifecycle
    //======================================================================
    protected override async Task OnParametersSetAsync()
        => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;

        try
        {
            _combatTaskDocument = await GetCombatTaskDetailsByDocumentIdQueryHandler.HandleAsync(
                new GetCombatTaskDetailsByDocumentIdQuery(DocumentId));

            if (_combatTaskDocument is null)
            {
                _missions = [];
                ToastService.Warning("Документ не знайдено або недоступний.");
                return;
            }

            _missions = [.. _combatTaskDocument.Missions
                // 1) сортуємо рядки всередині кожної місії
                .Select(m => m with
                {
                    CombatTaskDetails = [.. m.CombatTaskDetails
                        .OrderBy(d => d.EffectiveAt)
                        .ThenBy(d => d.Kind == CombatTaskDetailsKindDto.End ? 0 : 1)
                        .ThenBy(d => d.PersonId)                                 // щоб “по особам”
                        .ThenBy(d => d.FullName)
                        .ThenBy(d => d.CombatTaskDetailsId)]
                })
                // 2) сортуємо блоки місій по першій даті (хронологія)
                .OrderBy(m => m.CombatTaskDetails.Count == 0
                    ? DateOnly.MaxValue
                    : m.CombatTaskDetails.Min(d => d.EffectiveAt))
                .ThenBy(m => m.MissionName)];
        }
        finally
        {
            _loading = false;
        }
    }

    //======================================================================
    // Actions
    //======================================================================
    private void OpenStartForm()
        => NavigationManager.NavigateTo($"/task-document/{DocumentId}/start");

    private void OpenCloseForm()
        => NavigationManager.NavigateTo($"/task-document/{DocumentId}/close");

    //======================================================================
    // Helpers
    //======================================================================

    private static string KindStatus(DocumentStatusDto status)
        => status switch
        {
            DocumentStatusDto.Active => "Активний",
            DocumentStatusDto.Canceled => "Відмінений",
            _ => status.ToString()
        };

    private static string KindLabel(CombatTaskDetailsKindDto kind)
        => kind switch
        {
            CombatTaskDetailsKindDto.Start => "🟩 Почав виконання",
            CombatTaskDetailsKindDto.End => "🟨 Припинив виконання",
            _ => kind.ToString()
        };
}
