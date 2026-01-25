//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CombatTaskReadRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Read-repo для сторінок планування.
/// На цьому кроці — інфраструктура (порожні результати).
/// Далі підв’яжемо до реальних таблиць/сутностей (Documents/Lines/Assignments).
/// </summary>
public sealed class CombatTaskReadRepository : ICombatTaskReadRepository
{
    public async Task<IReadOnlyList<PlanningMonthAssignmentRowDto>> GetPlanningMonthAsync(
        int year, int month, string? search, CancellationToken ct = default)
        => await Task.FromResult<IReadOnlyList<PlanningMonthAssignmentRowDto>>([]);

    public async Task<IReadOnlyList<PlanningDocumentRowDto>> GetPlanningDocumentsAsync(
        int year, int month, CombatTaskPlanDocumentStatus? status, string? search, CancellationToken ct = default)
        => await Task.FromResult<IReadOnlyList<PlanningDocumentRowDto>>([]);

    public async Task<IReadOnlyList<PlanningDayGroupDto>> GetPlanningDayAsync(
        DateOnly date, string? search, CancellationToken ct = default)
        => await Task.FromResult<IReadOnlyList<PlanningDayGroupDto>>([]);
}