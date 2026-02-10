//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PostCombatTaskDocumentCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.CombatTask;

namespace eRaven.Application.Handlers.CombatTask;

public sealed class PostCombatTaskDocumentCommandHandler()
    : ICommandHandler<PostCombatTaskDocumentCommand>
{

    public async Task HandleAsync(PostCombatTaskDocumentCommand command, CancellationToken ct = default)
    {
    }
}