//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetMissionPlanningRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Read-репозиторій для задач планування/звітів по місіях, що базуються на фактах табеля.
/// </summary>
public interface ITimesheetMissionPlanningRepository
{
    /// <summary>
    /// Звіт: повертає перелік людей, які мають АКТИВНИЙ факт задачі по місії на дату <paramref name="onDate"/>.
    /// </summary>
    Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveMissionPersonsAsync(
        Guid missionId,
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Документ: кого можна завершити цим документом на дату endInclusive.
    /// Не повертає spans, які вже закриті іншим документом або reason-кодом.
    /// </summary>
    Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveMissionClosablePersonsAsync(
       Guid missionId,
       DateOnly onDate,
       CancellationToken ct = default);

    /// <summary>
    /// Повертає перелік людей, у яких факт задачі пов'язаний з документом (<c>openedBy</c> або <c>closedBy</c>)
    /// та є активним на дату <paramref name="onDate"/>.
    /// </summary>
    Task<IReadOnlyList<ActiveMissionPersonDto>> GetActiveMissionPersonsByDocumentAsync(
        Guid documentId,
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає перелік людей, які доступні для призначення задачі на дату <paramref name="onDate"/>.
    /// (Базова політика: останній код = ReadyToCombatTask та немає активної задачі на цю дату.)
    /// </summary>
    Task<IReadOnlyList<ReadyCombatTaskPersonDto>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        CancellationToken ct = default);
}
