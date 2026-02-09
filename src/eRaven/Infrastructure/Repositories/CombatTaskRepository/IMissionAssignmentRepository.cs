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
/// - Записи створюються/закриваються лише як наслідок "Posted" документів.
/// - Це read-оптимізована модель для звітів і швидких вибірок.
/// </summary>
public interface IMissionAssignmentRepository
{
    // ----------------------------
    // Reads
    // ----------------------------

    /// <summary>
    /// Хто активний на місії <paramref name="missionId"/> на дату <paramref name="onDate"/>.
    /// </summary>
    Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveByMissionAsync(
        Guid missionId,
        DateOnly onDate,
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
    /// Повертає людей, які НЕ мають активних призначень 
    /// на дату <paramref name="onDate"/> (вільні для нових місій).
    /// </summary>
    /// <param name="onDate"></param>
    /// <param name="ct"></param>
    Task<IReadOnlyList<ReadyCombatTaskPersonDto>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        bool includePlanned = false,
        CancellationToken ct = default);

    // ----------------------------
    // Write (internal)
    // ----------------------------

    /// <summary>
    /// Застосовує Draft-рядки Start/End.
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="ct"></param>
    Task ApplyDraftLinesAsync(IReadOnlyList<ApplyCombatTaskDetailsDto> lines,
        CancellationToken ct = default);

    /// <summary>
    /// Застосовує Posted-рядки Start/End як оновлення <see cref="MissionAssignment"/>.
    ///
    /// Очікуваний контракт:
    /// - Start: відкриває інтервал (From=EffectiveAt, To=null).
    /// - End: закриває інтервал (To=EffectiveAt) для відповідної місії.
    ///
    /// Будь-які конфлікти (End без Start, Start при вже відкритому інтервалі тощо)
    /// мають підійматись як винятки (handler перетворить у toast/validation).
    /// </summary>
    Task ApplyPostedLinesAsync(
        IReadOnlyList<ApplyCombatTaskDetailsDto> lines,
        CancellationToken ct = default);
}