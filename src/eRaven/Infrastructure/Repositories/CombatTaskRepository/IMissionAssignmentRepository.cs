//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IMissionAssignmentRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Репозиторій проєкційних фактів участі у місіях (<see cref="MissionAssignment"/>).
///
/// Примітки:
/// - MissionAssignment НЕ редагується напряму з UI.
/// - Це read-оптимізована модель для звітів і швидких вибірок.
/// </summary>
public interface IMissionAssignmentRepository
{
    // ----------------------------
    // Reads
    // ----------------------------

    /// <summary>
    /// Повертає людей, які НЕ мають активних призначень 
    /// на дату <paramref name="onDate"/> (вільні для нових місій).
    /// </summary>
    /// <param name="onDate"></param>
    /// <param name="ct"></param>
    Task<IReadOnlyList<ReadyCombatTaskPersonDto>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        bool includePlanned = false,
        CancellationToken ct = default);

    /// <summary>
    /// Хто активний на місії <paramref name="missionId"/> на дату <paramref name="onDate"/>.
    /// </summary>
    Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveByMissionAsync(
        Guid missionId,
        DateOnly onDate,
        bool includePlanned = false,
        CancellationToken ct = default);

    /// <summary>
    /// Історія призначень людини за період.
    /// </summary>
    Task<IReadOnlyList<MissionAssignment>> GetPersonAssignmentsAsync(
       Guid personId,
       DateOnly from,
       DateOnly to,
       bool includePlanned = false,
       CancellationToken ct = default);

    /// <summary>
    /// Поточна активна місія людини на дату <paramref name="onDate"/> (якщо є).
    /// </summary>
    Task<MissionAssignment?> GetActiveForPersonAsync(
        Guid personId,
        DateOnly onDate,
        bool includePlanned = false,
        CancellationToken ct = default);

    // ----------------------------
    // Write (internal)
    // ----------------------------

    /// <summary>
    /// Створює призначення на завдання згідно документа.
    /// 
    /// Стан призначення <see cref="MissionAssignment"/> MissionAssignment.Planned.
    /// </summary>
    /// <param name="taskDetails"></param>
    /// <param name="ct"></param>
    Task ApplyDraftCombatTaskDocumentAsync(IReadOnlyList<ApplyCombatTaskDetailsDto> taskDetails,
        CancellationToken ct = default);

    /// <summary>
    /// Оновлює <see cref="MissionAssignment"/> в стані Committed згідно документа.
    /// </summary>
    Task ApplyPostedCombatTaskDocumentAsync(Guid documentId, CancellationToken ct = default);
}