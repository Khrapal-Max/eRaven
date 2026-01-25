//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskReadRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public interface ICombatTaskReadRepository
{
    /// <summary>
    /// Повертає планування за місяць
    /// </summary>
    Task<IReadOnlyList<PlanningMonthAssignmentRowDto>> GetPlanningMonthAsync(
       int year,
       int month,
       string? search,
       CancellationToken ct = default);

    /// <summary>
    /// Повертає документи планування
    /// </summary>
    Task<IReadOnlyList<PlanningDocumentRowDto>> GetPlanningDocumentsAsync(
        int year,
        int month,
        CombatTaskPlanDocumentStatus? status,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає планування за день
    /// </summary>
    Task<IReadOnlyList<PlanningDayGroupDto>> GetPlanningDayAsync(
        DateOnly date,
        string? search,
        CancellationToken ct = default);
}
