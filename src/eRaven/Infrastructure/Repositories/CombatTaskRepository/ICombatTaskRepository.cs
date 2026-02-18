//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public interface ICombatTaskRepository
{
    // Read operations

    /// <summary>
    /// Повертає DTO редактора документа (header + місії + рядки).
    /// </summary>
    Task<CombatTaskEditorDto> GetDocumentEditorAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Повертає список MissionId, які присутні у документі.
    /// Використовується для легких документних операцій (наприклад Cancel),
    /// коли не потрібен повний editor DTO.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetDocumentMissionIdsAsync(Guid documentId, CancellationToken ct = default);

    // Write operations

    Task<Guid> CreateCombatTaskAsync(
        Guid documentId,
        Guid missionId,
        string sourceDocument,
        IReadOnlyCollection<CombatTaskDetails> combatTaskDetails,
        CancellationToken ct = default);

    Task<Guid> UpsertCombatTaskAsync(
        Guid documentId,
        Guid missionId,
        string sourceDocument,
        IReadOnlyCollection<CombatTaskDetails> combatTaskDetails,
        CancellationToken ct = default);

    Task DeleteCombatTaskAsync(
        Guid documentId,
        Guid combatTaskId,
        CancellationToken ct = default);
}
