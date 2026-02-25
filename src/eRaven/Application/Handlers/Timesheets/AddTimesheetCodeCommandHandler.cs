//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// AddTimesheetCodeCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Хендлер створення коду епізода табеля для довідника
/// </summary>
public sealed class AddTimesheetCodeCommandHandler(
    ITimesheetPolicyRepository repo)
    : ICommandHandler<AddTimesheetCodeCommand, Guid>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    ///  <inheritdoc/>
    public async Task<Guid> HandleAsync(AddTimesheetCodeCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var author = (command.Author ?? string.Empty).Trim();

        return await _repo.AddCodeAsync(
            code: command.Code,
            title: command.Title,
            description: command.Description,
            sortOrder: command.SortOrder,
            priority: command.Priority,
            isTerminal: command.IsTerminal,
            roleCode: command.RoleCode,
            uiStyle: command.UiStyle,
            author: author,
            nowUtc: command.NowUtc,
            ct: ct);
    }
}