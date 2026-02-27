//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskMissionAssignmentQueryRepository
//-----------------------------------------------------------------------------

namespace eRaven.Application.Abstractions.CombatTaskRepository;

/// <summary>
/// Query-репозиторій для планування/звітів по місіях (CombatTask).
///
/// <para>
/// Джерело правди по зайнятості на завданнях — CombatTask. Репозиторій читає матеріалізовані інтервали
/// призначень (<c>MissionAssignment</c>) та повертає лише <see cref="Guid"/> ідентифікатори осіб.
/// </para>
///
/// <para>
/// Семантика інтервалів: half-open <c>[From..To)</c>. Якщо <c>To == null</c> — призначення відкрите.
/// </para>
///
/// <para>
/// Важливо: репозиторій <b>не</b> залежить від табеля та не перевіряє табельні коди.
/// Обчислення "eligible-by-timesheet" та "free" (eligible − occupied) виконується в оркестраторі/хендлері.
/// </para>
/// </summary>
public interface ICombatTaskMissionAssignmentQueryRepository
{
    /// <summary>
    /// Звіт: повертає перелік людей, які мають активне призначення по місії на дату <paramref name="onDate"/>.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveMissionPersonsAsync(
        Guid missionId,
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає мінімальну дату початку активного призначення по місії для кожної особи на дату <paramref name="onDate"/>.
    ///
    /// <para>
    /// Призначення: побудова UI для "закінчити завдання" (показати початок інтервалу) або звіти.
    /// </para>
    /// </summary>
    Task<IReadOnlyDictionary<Guid, DateOnly>> GetActiveMissionPersonFromDatesAsync(
        Guid missionId,
        DateOnly onDate,
        IReadOnlyCollection<Guid> personIds,
        CancellationToken ct = default);

    /// <summary>
    /// Документ: повертає перелік людей, які мають активне призначення, пов'язане зі стартом з документа
    /// <paramref name="documentId"/>, та є активними на дату <paramref name="onDate"/>.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetActiveMissionPersonsByDocumentAsync(
        Guid documentId,
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає перелік людей, які мають <b>відкриті</b> призначення (To == null) станом на дату <paramref name="onDate"/>.
    ///
    /// <para>
    /// Використання: "Закінчити завдання" (список тих, хто зараз на завданні взагалі).
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Guid>> GetPersonsWithOpenAssignmentsAsync(
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає перелік людей, які зайняті будь-яким призначенням у діапазоні <c>[from..toExclusive)</c>.
    ///
    /// <para>
    /// Використання: формування кандидатів для нового документа (blocked persons),
    /// враховуючи політику зміщення (shift) на рівні оркестратора.
    /// </para>
    /// </summary>
    Task<IReadOnlyList<Guid>> GetOccupiedPersonsInRangeAsync(
        DateOnly fromDate,
        DateOnly toExclusive,
        CancellationToken ct = default);
}
