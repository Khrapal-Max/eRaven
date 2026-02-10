//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DocumentEditor
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Presentation.Toasts;
using Microsoft.AspNetCore.Components;

namespace eRaven.Components.Pages.CombatTask;

public partial class DocumentEditor : ComponentBase
{
    //======================================================================
    // DI
    //======================================================================
    [Inject] public IQueryHandler<GetCombatTaskDetailsByDocumentIdQuery, CombatTaskEditorDto?> GetCombatTaskHandler { get; set; } = default!;
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
    private bool IsNotDraft => (_combatTaskDocument?.Status) != DocumentStatus.Draft;
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
            _combatTaskDocument = await GetCombatTaskHandler.HandleAsync(
                new GetCombatTaskDetailsByDocumentIdQuery(DocumentId));

            if (_combatTaskDocument is null)
            {
                _missions = [];
                Toasts.Warning("Документ не знайдено або недоступний.");
                return;
            }

            _missions = [.. _combatTaskDocument.Missions
                // 1) сортуємо рядки всередині кожної місії
                .Select(m => m with
                {
                    CombatTaskDetails = m.CombatTaskDetails
                        .OrderBy(d => d.EffectiveAt)
                        .ThenBy(d => d.Kind == CombatTaskDetailsKind.End ? 0 : 1) // End before Start
                        .ThenBy(d => d.PersonId)                                 // щоб “по особам”
                        .ThenBy(d => d.FullName)
                        .ThenBy(d => d.CombatTaskDetailsId)
                        .ToList()
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
    => Nav.NavigateTo($"/task-document/{DocumentId}/start");

    private void OpenCloseForm()
        => Nav.NavigateTo($"/task-document/{DocumentId}/close");

    //======================================================================
    // Helpers
    //======================================================================

    private static string KindLabel(CombatTaskDetailsKind kind)
        => kind switch
        {
            CombatTaskDetailsKind.Start => "🟩 Почав виконання",
            CombatTaskDetailsKind.End => "🟨 Припинив виконання",
            _ => kind.ToString()
        };
}
