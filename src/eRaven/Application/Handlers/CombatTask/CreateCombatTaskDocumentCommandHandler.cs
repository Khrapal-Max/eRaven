//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDocumentCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;

namespace eRaven.Application.Handlers.CombatTask;

/// <summary>
/// Хендлер створення документа бойових завдань.
///
/// <para>
/// У спрощеній моделі документ одразу створюється як чинний (<c>Active</c>)
/// і виступає підставою для формування факту у табелі (наступними командами).
/// </para>
/// </summary>
public sealed class CreateCombatTaskDocumentCommandHandler(
    ICombatTaskDocumentRepository repo)
    : ICommandHandler<CreateCombatTaskDocumentCommand, Guid>
{
    private readonly ICombatTaskDocumentRepository _repo = repo;

    /// <inheritdoc />
    public async Task<Guid> HandleAsync(CreateCombatTaskDocumentCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        // IMPORTANT:
        // Репозиторій приймає саме DateTime (UTC) для audit полів.
        return await _repo.CreateAsync(
            orderTitle: command.OrderTitle,
            recordedAt: command.RecordedAt,
            description: command.Description,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
