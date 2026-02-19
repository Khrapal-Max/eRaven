//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CancelCombatTaskDocumentCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTasks;
using eRaven.Infrastructure;

namespace eRaven.Application.Handlers.CombatTasks;

/// <summary>
/// Хендлер скасування (Cancel/Void) документа бойових завдань:
/// <list type="bullet">
/// <item><description>Скасовує документ у репозиторії документів.</description></item>
/// <item><description>Компенсує факти табеля (TaskSpans) для всіх місій документа.</description></item>
/// </list>
/// </summary>
public sealed class CancelCombatTaskDocumentCommandHandler(
    ICombatTaskDocumentRepository documents,
    ICombatTaskRepository combatTasks,
    ITimesheetAggregateRepository timesheets,
    ITimesheetPolicyRepository policy)
    : ICommandHandler<CancelCombatTaskDocumentCommand, Guid>
{
    private readonly ICombatTaskDocumentRepository _documents = documents;
    private readonly ICombatTaskRepository _combatTasks = combatTasks;
    private readonly ITimesheetAggregateRepository _timesheets = timesheets;
    private readonly ITimesheetPolicyRepository _policy = policy;

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

        // 1) Скасувати документ (компенсація на рівні документа)
        await _documents.CancelAsync(
            documentId: command.DocumentId,
            reason: string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim(),
            author: command.Author.Trim(),
            nowUtc: command.NowUtc,
            ct: ct);

        // 2) Знайти місії документа (без введення нових контрактів — через DTO editor)
        var editor = await _combatTasks.GetDocumentAsync(command.DocumentId, ct);

        var missionIds = editor.CombatTasks
            .Select(m => m.MissionId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (missionIds.Count == 0)
            return command.DocumentId;

        // 3) reasonCodeId для скасування span:
        //    беремо існуючий код "100" (DoesTheCombatTask) з політики.
        var codes = await _policy.GetCodesAsync(includeInactive: true, ct);

        var reasonCodeId = codes
            .FirstOrDefault(x => string.Equals((x.Code ?? string.Empty).Trim(),
                TimesheetSystemCodes.DoesTheCombatTask,
                StringComparison.OrdinalIgnoreCase))
            ?.Id
            ?? throw new InvalidOperationException("Не знайдено код '100' (DoesTheCombatTask) у політиці табеля.");

        // 4) Компенсація фактів у табелі по кожній місії
        foreach (var missionId in missionIds)
        {
            await _timesheets.CancelCombatTaskFactsAsync(
                documentId: command.DocumentId,
                missionId: missionId,
                reasonCodeId: reasonCodeId,
                reference: string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim(),
                author: command.Author.Trim(),
                nowUtc: command.NowUtc,
                ct: ct);
        }

        return command.DocumentId;
    }
}
