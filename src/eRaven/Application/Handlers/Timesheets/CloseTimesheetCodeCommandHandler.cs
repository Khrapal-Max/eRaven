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

public sealed class CloseTimesheetCodeCommandHandler(
    ITimesheetPolicyRepository repo)
    : ICommandHandler<CloseTimesheetCodeCommand>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    public async Task HandleAsync(CloseTimesheetCodeCommand command, CancellationToken ct = default)
        => await _repo.CloseCodeAsync(
            codeId: command.CodeId,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
}
