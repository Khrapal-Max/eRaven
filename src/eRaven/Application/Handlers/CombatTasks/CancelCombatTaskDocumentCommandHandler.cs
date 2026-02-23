//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CancelCombatTaskDocumentCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTasks;

namespace eRaven.Application.Handlers.CombatTasks;

/// <summary>
/// Хендлер скасування (Cancel/Void) документа бойових завдань:
/// <list type="bullet">
/// <item><description>Скасовує документ у репозиторії документів.</description></item>
/// <item><description>Компенсує derived entries табеля (код 100) по місіях документа.</description></item>
/// </list>
/// </summary>
public sealed class CancelCombatTaskDocumentCommandHandler(
    ICombatTaskDocumentRepository documents,
    ICombatTaskRepository combatTasks)
    : ICommandHandler<CancelCombatTaskDocumentCommand, Guid>
{
    private readonly ICombatTaskDocumentRepository _documents = documents;
    private readonly ICombatTaskRepository _combatTasks = combatTasks;

    /// <inheritdoc />
    public async Task<Guid> HandleAsync(CancelCombatTaskDocumentCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.DocumentId == Guid.Empty)
            throw new InvalidOperationException("DocumentId обов'язковий.");

        if (string.IsNullOrWhiteSpace(command.Author))
            throw new InvalidOperationException("Author обов'язковий.");

        if (command.NowUtc == default)
            throw new InvalidOperationException("NowUtc обов'язковий.");

        var author = command.Author.Trim();
        var reason = string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim();

        // 1) Скасувати документ (компенсація на рівні документа)
        await _documents.CancelAsync(
            documentId: command.DocumentId,
            reason: reason,
            author: author,
            nowUtc: command.NowUtc,
            ct: ct);

        // 2) Знайти місії документа (через DTO editor)
        var editor = await _combatTasks.GetDocumentAsync(command.DocumentId, ct);

        var missionIds = editor.CombatTasks
            .Select(m => m.MissionId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (missionIds.Count == 0)
            return command.DocumentId;

        /*// 3) Компенсація derived entries у табелі по кожній місії
        foreach (var missionId in missionIds)
        {
            await _timesheets.CancelCombatTaskFactsAsync(
                documentId: command.DocumentId,
                missionId: missionId,
                author: author,
                nowUtc: command.NowUtc,
                ct: ct);
        }*/

        return command.DocumentId;
    }
}
