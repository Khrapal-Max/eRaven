//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SaveTimesheetPolicyCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheet;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class SaveTimesheetPolicyCommandHandler(
    ITimesheetPolicyRepository repo)
    : ICommandHandler<SaveTimesheetPolicyCommand>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    public async Task HandleAsync(SaveTimesheetPolicyCommand command, CancellationToken ct = default)
        => await _repo.SavePolicyAsync(
            codeId: command.CodeId,
            title: command.Title,
            description: command.Description,
            sortOrder: command.SortOrder,
            priority: command.Priority,
            isTerminal: command.IsTerminal,
            allowedTransitions: command.AllowedTransitions,
            author: command.Author,
            nowUtc: command.NowUtc,
            ct: ct);
}
