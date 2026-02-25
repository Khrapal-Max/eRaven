//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CloseTimesheetCodeCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Хендлер закриття коду епізода табеля.
/// </summary>
public sealed class CloseTimesheetCodeCommandHandler(
    ITimesheetPolicyRepository repo)
    : ICommandHandler<CloseTimesheetCodeCommand>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    ///  <inheritdoc/>
    public async Task HandleAsync(CloseTimesheetCodeCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CodeId == Guid.Empty)
            throw new InvalidOperationException("CodeId обов'язковий.");

        var author = (command.Author ?? string.Empty).Trim();

        await _repo.CloseCodeAsync(
            codeId: command.CodeId,
            author: author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}