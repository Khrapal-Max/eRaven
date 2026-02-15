//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// CRUD-репозиторій контенту документа бойових завдань:
/// <list type="bullet">
/// <item><description><see cref="CombatTask"/> — блоки по місіях у межах документа;</description></item>
/// <item><description><see cref="CombatTaskDetails"/> — snapshot-рядки (Start/End) у межах місії.</description></item>
/// </list>
///
/// <para>
/// Важливо: бізнес-правила (Draft/Posted/Canceled, блокування табельних подій, apply в <c>TimesheetTaskSpan</c>
/// та проєкції <c>MissionAssignment</c>) реалізуються в handler’ах/агрегатах.
/// Репозиторій відповідає лише за цілісність зв’язків та коректний запис даних.
/// </para>
/// </summary>
public interface ICombatTaskRepository
{
    // Read operations

    /// <summary>
    /// Повертає DTO редактора документа (header + місії + рядки).
    /// </summary>
    Task<CombatTaskEditorDto> GetDocumentEditorAsync(Guid documentId, CancellationToken ct = default);

    // Write operations

    /// <summary>
    /// Створює новий блок по місії у межах документа (новий <see cref="CombatTask"/>)
    /// та додає рядки <see cref="CombatTaskDetails"/>.
    ///
    /// <para>Примітка:</para>
    /// <list type="bullet">
    /// <item><description>Якщо блок по цій місії вже існує — метод кидає виняток.</description></item>
    /// <item><description>Для ідемпотентного оновлення/перезапису використовуйте <see cref="UpsertCombatTaskAsync"/>.</description></item>
    /// </list>
    /// </summary>
    Task<Guid> CreateCombatTaskAsync(
        Guid documentId,
        Guid missionId,
        string sourceDocument,
        IReadOnlyCollection<CombatTaskDetails> combatTaskDetails,
        CancellationToken ct = default);

    /// <summary>
    /// Ідемпотентно створює або оновлює блок по місії у межах документа:
    /// <list type="bullet">
    /// <item><description>якщо блоку немає — створює <see cref="CombatTask"/>;</description></item>
    /// <item><description>якщо блок є — оновлює <c>SourceDocument</c> і ЗАМІНЮЄ всі рядки (<see cref="CombatTaskDetails"/>).</description></item>
    /// </list>
    ///
    /// <para>Сценарій:</para>
    /// <list type="bullet">
    /// <item><description>“Повернути”: табель віддає список людей → документ отримує snapshot-рядки без пошуків/пари-логіки.</description></item>
    /// </list>
    /// </summary>
    Task<Guid> UpsertCombatTaskAsync(
        Guid documentId,
        Guid missionId,
        string sourceDocument,
        IReadOnlyCollection<CombatTaskDetails> combatTaskDetails,
        CancellationToken ct = default);

    /// <summary>
    /// Видаляє блок місії (CombatTask) у межах документа.
    /// Всі рядки (<see cref="CombatTaskDetails"/>) видаляються каскадно.
    /// </summary>
    Task DeleteCombatTaskAsync(
        Guid documentId,
        Guid combatTaskId,
        CancellationToken ct = default);
}
