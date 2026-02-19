//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// SaveTimesheetPolicyCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheets;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Domain.ValueObjects;

namespace eRaven.Application.Handlers.Timesheets;

public sealed class SaveTimesheetPolicyCommandHandler(ITimesheetPolicyRepository repo)
    : ICommandHandler<SaveTimesheetPolicyCommand>
{
    private readonly ITimesheetPolicyRepository _repo = repo;

    public async Task HandleAsync(SaveTimesheetPolicyCommand command, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var title = (command.Title ?? string.Empty).Trim();
        var description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description.Trim();

        if (command.CodeId == Guid.Empty) throw new InvalidOperationException("CodeId обов'язковий.");
        if (string.IsNullOrWhiteSpace(title)) throw new InvalidOperationException("Назва коду не може бути порожньою.");
        if (command.SortOrder < 0) throw new InvalidOperationException("SortOrder не може бути < 0.");
        if (command.Priority < 0) throw new InvalidOperationException("Priority не може бути < 0.");
        if (string.IsNullOrWhiteSpace(command.Author)) throw new InvalidOperationException("Author is required.");
        if (command.NowUtc == default) throw new InvalidOperationException("NowUtc is required.");

        var normalized = NormalizeTransitions(command.CodeId, command.AllowedTransitions);

        await _repo.SavePolicyAsync(
            codeId: command.CodeId,
            title: title,
            description: description,
            sortOrder: command.SortOrder,
            priority: command.Priority,
            isTerminal: command.IsTerminal,
            allowedTransitions: normalized,
            author: command.Author.Trim(),
            nowUtc: command.NowUtc,
            ct: ct);
    }

    private static List<TimesheetTransitionSpec> NormalizeTransitions(
        Guid fromCodeId,
        IReadOnlyCollection<TimesheetTransitionSpecDto>? transitions)
    {
        transitions ??= [];

        var normalized = transitions
            .Where(x => x is not null)
            .Where(x => x.ToCodeId != Guid.Empty)
            .Where(x => x.ToCodeId != fromCodeId)
            .Select(x => new TimesheetTransitionSpec(x.ToCodeId, x.StartShiftDays))
            .GroupBy(x => x.ToCodeId)
            .Select(g => g.First())
            .ToList();

        foreach (var t in normalized)
        {
            if (t.StartShiftDays is < 0 or > 7)
                throw new InvalidOperationException("StartShiftDays має бути в межах 0..7 (для карти зазвичай 0 або 1).");
        }

        return normalized;
    }
}
