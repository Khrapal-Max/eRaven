//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateCombatTaskDocumentCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.CombatTaskRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTasks;

namespace eRaven.Application.Handlers.CombatTasks;

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

        if (string.IsNullOrWhiteSpace(command.OrderTitle))
            throw new ArgumentException("OrderTitle is required.", nameof(command));

        if (string.IsNullOrWhiteSpace(command.Author))
            throw new ArgumentException("Author is required.", nameof(command));

        if (command.RecordedAt == default)
            throw new ArgumentException("RecordedAt is required.", nameof(command));

        if (command.NowUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException("NowUtc must be UTC.", nameof(command));

        var orderTitle = command.OrderTitle.Trim();
        var author = command.Author.Trim();
        var description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();

        // IMPORTANT:
        // Репозиторій приймає саме DateTime (UTC) для audit полів.
        return await _repo.CreateAsync(
            orderTitle: orderTitle,
            recordedAt: command.RecordedAt,
            description: description,
            author: author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}
