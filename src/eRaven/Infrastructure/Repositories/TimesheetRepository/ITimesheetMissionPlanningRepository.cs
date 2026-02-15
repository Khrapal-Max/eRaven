//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetMissionPlanningRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Read-репозиторій планування місій на основі табеля (TimesheetTaskSpan).
/// </summary>
public interface ITimesheetMissionPlanningRepository
{
    /// <summary>
    /// Повертає людей, які в табелі на дату <paramref name="onDate"/> та не зайняті активним TaskSpan.
    /// </summary>
    Task<IReadOnlyList<ReadyCombatTaskPersonDto>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає людей, активних на місії на дату за даними TaskSpan.
    /// </summary>
    Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveByMissionAsync(
        Guid missionId,
        DateOnly onDate,
        bool includeDraft,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає людей, активних у документі на дату (для "повернути"/rollback).
    /// </summary>
    Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveByDocumentAsync(
        Guid documentId,
        DateOnly onDate,
        bool includeDraft,
        CancellationToken ct = default);
}
