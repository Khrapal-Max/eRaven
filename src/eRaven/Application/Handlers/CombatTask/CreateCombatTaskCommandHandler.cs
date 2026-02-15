//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;
using eRaven.Domain.Entities;
using eRaven.Infrastructure.Repositories.CombatTaskRepository;

namespace eRaven.Application.Handlers.CombatTask;

/// <summary>
/// Створює блок місії (<see cref="CombatTask"/>) у межах документа бойових завдань
/// та зберігає snapshot-рядки (<see cref="CombatTaskDetails"/>).
///
/// <para>
/// Примітка: цей хендлер відповідає лише за контент документа (editor CRUD).
/// Планування (Draft) у табелі через <c>TimesheetTaskSpan</c> та формування факту
/// <c>MissionAssignment</c> при Posted виконуються окремими командами/хендлерами.
/// </para>
/// </summary>
public sealed class CreateCombatTaskCommandHandler(ICombatTaskRepository repo)
    : ICommandHandler<CreateCombatTaskCommand, Guid>
{
    private readonly ICombatTaskRepository _repo = repo;

    /// <inheritdoc />
    public async Task<Guid> HandleAsync(CreateCombatTaskCommand command, CancellationToken ct = default)
    {
        if (command.DocumentId == Guid.Empty)
            throw new ArgumentException("DocumentId is required.", nameof(command.DocumentId));

        if (command.MissionId == Guid.Empty)
            throw new ArgumentException("MissionId is required.", nameof(command.MissionId));

        var details = command.CombatTaskDetails
            .Select(x => new CombatTaskDetails
            {
                // Id може бути порожнім — репозиторій має право згенерувати його при insert.
                Id = x.CombatTaskDetailsId,
                CombatTaskId = Guid.Empty,   // буде проставлено репозиторієм
                CombatTask = null,           // не тягнемо граф
                Kind = x.Kind,
                EffectiveAt = x.EffectiveAt,
                PersonId = x.PersonId,
                Rnokpp = x.Rnokpp,
                FullName = x.FullName,
                Callsign = x.Callsign
            })
            .ToList();

        // Створюємо блок місії в документі (без будь-яких проєкцій/табеля)
        var combatTaskId = await _repo.CreateCombatTaskAsync(
            documentId: command.DocumentId,
            missionId: command.MissionId,
            sourceDocument: command.SourceDocument,
            combatTaskDetails: details,
            ct: ct);

        return combatTaskId;
    }
}
