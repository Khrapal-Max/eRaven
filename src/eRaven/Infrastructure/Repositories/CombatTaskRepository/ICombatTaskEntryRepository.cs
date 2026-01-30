//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskEntryRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public interface ICombatTaskEntryRepository
{
    /// <summary>
    /// Додає запис місії з групою осіб.
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
    /// Видаляє запис місії з групою осіб.
    /// </summary>
    Task DeleteGroupAsync(Guid documentId, Guid groupId, CancellationToken ct = default);

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
    /// Замінює склад осіб у групі.
    /// UI надсилає "фінальний" список осіб, репозиторій робить diff:
    /// - видаляє зайвих
    /// - додає відсутніх (зі snapshot полів з PersonRead)
    /// Якщо список порожній — група буде видалена (тобто всі рядки групи).
    /// </summary>
    Task ReplacePersonsInGroupAsync(
        Guid documentId,
        Guid groupId,
        IReadOnlyCollection<Guid> personIds,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}