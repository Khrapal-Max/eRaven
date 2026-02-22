//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetMissionPlanningRepository
//-----------------------------------------------------------------------------

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Read-репозиторій для планування/звітів по місіях.
///
/// <para>
/// Табель <b>не</b> є джерелом правди по завданнях. Джерело правди — CombatTask.
/// Для звітів використовується матеріалізована read-модель призначень (<c>MissionAssignment</c>).
/// </para>
///
/// <para>
/// Репозиторій повертає лише <see cref="Guid"/> ідентифікатори людей. Збір DTO виконується в хендлері
/// (через читання PersonRead/Document за потреби).
/// </para>
/// </summary>
public interface ITimesheetMissionPlanningRepository
{
    /// <summary>
    /// Звіт: повертає перелік людей, які мають АКТИВНЕ призначення по місії на дату <paramref name="onDate"/>.
    /// Дозволено кілька документів в один день.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveMissionPersonsAsync(
        Guid missionId,
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає мінімальну дату початку активного призначення по місії для кожної особи
    /// на дату <paramref name="onDate"/>.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, DateOnly>> GetActiveMissionPersonFromDatesAsync(
        Guid missionId,
        DateOnly onDate,
        IReadOnlyCollection<Guid> personIds,
        CancellationToken ct = default);


    /// <summary>
    /// Документ: повертає перелік людей, які мають АКТИВНЕ призначення, пов'язане з документом
    /// <paramref name="documentId"/>, та є активними на дату <paramref name="onDate"/>.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveMissionPersonsByDocumentAsync(
        Guid documentId,
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає перелік людей, які можуть бути призначені на завдання на дату <paramref name="onDate"/>.
    ///
    /// <para>
    /// Базова політика:
    /// <list type="bullet">
    /// <item><description>Поточний табельний код на дату <paramref name="onDate"/> ∈ {30, 100}.</description></item>
    /// <item><description>Відсутні <b>відкриті</b> призначення (MissionAssignment.To == null) на будь-яку місію.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Guid>> GetFreePersonForMissionsAsync(
        DateOnly onDate,
        CancellationToken ct = default);
}
