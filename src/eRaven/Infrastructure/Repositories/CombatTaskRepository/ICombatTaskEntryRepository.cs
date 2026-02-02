//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskEntryRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Репозиторій рядків документа (CombatTaskEntry).
///
/// Призначення (мінімально необхідне для UI):
/// - CRUD по "групі" (GroupId) в межах одного документа
/// - Повернення списків осіб:
///   - хто зараз НА місії (для End)
///   - хто зараз ВІЛЬНИЙ (для Start)
///
/// Джерело істини для стану:
/// - останній запис по особі на дату (ActionDate <= asOfDate), ORDER BY RowNo DESC.
/// </summary>
public interface ICombatTaskEntryRepository
{
    /// <summary>
    /// Повертає список осіб, які на дату <paramref name="asOfDate"/> знаходяться "на завданні"
    /// і їх останній Start вказує на <paramref name="missionId"/>.
    /// Використовується для швидкого вибору осіб у режимі End.
    /// </summary>
    Task<IReadOnlyList<CombatTaskPersonLookupDto>> GetPersonsOnMissionAsync(
        Guid missionId,
        DateOnly asOfDate,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає список осіб, які на дату <paramref name="asOfDate"/> є вільними (не мають активного Start).
    /// Використовується для швидкого вибору осіб у режимі Start.
    /// </summary>
    Task<IReadOnlyList<CombatTaskPersonLookupDto>> GetFreePersonsAsync(
        Guid documentId,
        DateOnly asOfDate,
        Guid? excludeGroupId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Створює групу (GroupId) і додає по одному рядку на кожну особу.
    /// </summary>
    Task<Guid> AddGroupAsync(
        Guid documentId,
        string sourceDocNo,
        ActionKind action,
        Guid missionId,
        string missionDisplaySnapshot,
        DateOnly actionDate,
        IReadOnlyCollection<Guid> personIds,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Видаляє всі рядки групи в документі.
    /// </summary>
    Task DeleteGroupAsync(
        Guid documentId,
        Guid groupId,
        CancellationToken ct = default);

    /// <summary>
    /// Оновлює метадані групи (без зміни складу осіб).
    /// Оновлює всі рядки групи однаково.
    /// </summary>
    Task UpdateGroupAsync(
        Guid documentId,
        Guid groupId,
        string sourceDocNo,
        ActionKind action,
        Guid missionId,
        string missionDisplaySnapshot,
        DateOnly actionDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Замінює склад осіб у групі (GroupId) в межах документа.
    /// Сnapshots для нових осіб беруться з PersonRead на момент операції.
    /// </summary>
    Task ReplaceGroupPersonsAsync(
        Guid documentId,
        Guid groupId,
        IReadOnlyCollection<Guid> personIds,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
