//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IMissionAssignmentRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Репозиторій проєкційних ФАКТІВ участі у місіях (<see cref="MissionAssignment"/>).
/// </summary>
public interface IMissionAssignmentRepository
{
    /// <summary>
    /// Історія призначень людини за період (overlap).
    /// </summary>
    Task<IReadOnlyList<MissionAssignment>> GetPersonAssignmentsAsync(
        Guid personId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default);

    /// <summary>
    /// Поточний активний факт участі людини на дату (якщо є).
    /// </summary>
    Task<MissionAssignment?> GetActiveForPersonAsync(
        Guid personId,
        DateOnly onDate,
        CancellationToken ct = default);

    /// <summary>
    /// Створює/оновлює факти для Posted документа на основі табельних <c>TimesheetTaskSpan(Status=Posted)</c>.
    /// </summary>
    Task ApplyPostedCombatTaskDocumentAsync(
        Guid documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Закриває факт участі по ключу (StartDocumentId, MissionId, PersonId).
    /// </summary>
    /// <param name="startDocumentId">Документ, який створив факт (джерело старту).</param>
    /// <param name="missionId">Місія.</param>
    /// <param name="personId">Особа.</param>
    /// <param name="closeAt">Дата завершення (inclusive).</param>
    /// <param name="closedByDocumentId">
    /// Документ, яким виконано завершення (якщо закриття штатне).
    /// Для аварійних кодів (Ф100/200 тощо) — <c>null</c>.
    /// </param>
    Task CloseAsync(
        Guid startDocumentId,
        Guid missionId,
        Guid personId,
        DateOnly closeAt,
        Guid? closedByDocumentId,
        CancellationToken ct = default);
}