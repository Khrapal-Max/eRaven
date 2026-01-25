//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskAssignmentRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

public interface ICombatTaskAssignmentRepository
{
    /// <summary>
    /// Повертає відкриті таски по особі
    /// </summary>
    Task<CombatTaskAssignment?> GetOpenAssignmentForPersonAsync(
        Guid personId,
        CancellationToken ct = default);

    /// <summary>
    /// Створює таску
    /// </summary>
    Task CreateAssignmentAsync(
        CombatTaskAssignment assignment,
        CancellationToken ct = default);

    /// <summary>
    /// Закриває таску
    /// </summary>
    Task CloseAssignmentAsync(
        Guid assignmentId,
        DateOnly endedAt,
        Guid endDocumentId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}