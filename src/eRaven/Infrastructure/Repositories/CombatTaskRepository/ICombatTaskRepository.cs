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
/// Репозиторій планування бойових завдань.
/// 
/// Призначення:
/// - Завдання = "пакет вводу" (одне завдання містить багато осіб/рядків Start/End). 
/// </summary>
public interface ICombatTaskRepository
{
    // Read operations

    /// <summary>
    /// Повертає всі бойові завдання, які належать до певного документа.
    /// </summary>
    /// <param name="documentId"></param
    Task<CombatTaskEditorDto> GetDocumentEditorAsync(Guid documentId, CancellationToken ct = default);

    // Write operations
    /// <summary>
    /// Створює нове бойове завдання, яке належить до певного документа та місії,
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="missionId"></param>
    /// <param name="combatTaskDetails"></param
    Task<Guid> CreateCombatTask(Guid documentId, Guid missionId, string sourceDocument,
        ICollection<CombatTaskDetails> combatTaskDetails, CancellationToken ct = default);

    /// <summary>
    /// Видаляє бойове завдання, яке належить до певного документа та місії,
    /// </summary>
    /// <param name="documentId"></param>
    /// <param name="combatTaskId"></param>
    Task DeleteCombatTask(Guid documentId, Guid combatTaskId,
        CancellationToken ct = default);
}
