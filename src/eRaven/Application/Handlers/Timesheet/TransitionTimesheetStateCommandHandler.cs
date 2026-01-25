//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TransitionTimesheetStateCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class TransitionTimesheetStateCommandHandler(
    ITimesheetEntryRepository repo,
    ITimesheetPolicyRepository policyRepo)
    : ICommandHandler<TransitionTimesheetStateCommand>
{
    private readonly ITimesheetEntryRepository _repo = repo;
    private readonly ITimesheetPolicyRepository _policyRepo = policyRepo;

    public async Task HandleAsync(TransitionTimesheetStateCommand command, CancellationToken ct = default)
    {
        if (command.PersonId == Guid.Empty) throw new ArgumentException("PersonId is required.", nameof(command.PersonId));
        if (string.IsNullOrWhiteSpace(command.NextCode)) throw new ArgumentException("NextCode is required.", nameof(command.NextCode));

        var author = string.IsNullOrWhiteSpace(command.Author) ? "system" : command.Author.Trim();
        var nextCodeNorm = NormalizeCode(command.NextCode);

        // 1) anchor: current active state on AnchorDate
        var prev = await _repo.GetActiveEntryOnDateAsync(command.PersonId, command.Lane, command.AnchorDate, ct)
            ?? throw new InvalidOperationException("Не можна застосувати перехід: на цю дату немає активної події (немає історії / поза табелем).");

        // 2) load policy definitions for lane
        var defs = await _policyRepo.GetCodesAsync(command.Lane, ct);

        var prevCodeNorm = NormalizeCode(prev.Code);
        var prevDef = defs.FirstOrDefault(x => NormalizeCode(x.Code) == prevCodeNorm)
            ?? throw new InvalidOperationException($"Policy: не знайдено код '{prevCodeNorm}' для lane {command.Lane}.");

        var nextDef = defs.FirstOrDefault(x => NormalizeCode(x.Code) == nextCodeNorm)
            ?? throw new InvalidOperationException($"Policy: не знайдено код '{nextCodeNorm}' для lane {command.Lane}.");

        // 3) policy graph: prev => next must be reachable (same-code allowed)
        if (prevDef.Id != nextDef.Id)
        {
            var ok = await IsReachableAsync(prevDef.Id, nextDef.Id, ct);
            if (!ok)
                throw new InvalidOperationException($"Перехід '{prevCodeNorm}' до '{nextCodeNorm}' заборонений політикою.");
        }

        // 4) Interpret user's InputDate by CURRENT state's meaning (prevDef.EndDateMeaning)
        DateOnly prevLastDay;
        DateOnly nextFrom;

        if (prevDef.EndDateMeaning == TimesheetEndDateMeaning.LastDayOfThisCode)
        {
            // InputDate = last day of current code
            prevLastDay = command.InputDate;
            nextFrom = command.InputDate.AddDays(1);
        }
        else // FirstDayOfNextCode
        {
            // InputDate = first day of next code (return/effective date)
            nextFrom = command.InputDate;
            prevLastDay = command.InputDate.AddDays(-1);
        }

        // 5) Validate boundaries (no silent "cancel")
        if (prevLastDay < prev.From)
            throw new InvalidOperationException("Неможливо: дата завершення поточного стану раніше початку поточної події.");

        // (додатково можна заборонити "обрізати" так, щоб anchor став поза діапазоном)
        // if (command.AnchorDate > prevLastDay) throw ...

        // 6) Update prev (close it)
        prev.To = prevLastDay;
        prev.UpdatedBy = author;
        prev.UpdatedAtUtc = command.NowUtc;

        // 7) Add next as open-ended
        var next = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimelineId = prev.TimelineId,
            PersonId = command.PersonId,
            Lane = command.Lane,
            Code = nextCodeNorm,
            From = nextFrom,
            To = null,
            Reference = TrimOrNull(command.Reference),
            Note = TrimOrNull(command.Note),
            CreatedBy = author,
            CreatedAtUtc = command.NowUtc
        };

        // 8) Atomic save (update+insert)
        await _repo.SaveTransitionAsync(prev, next, ct);
    }

    private async Task<bool> IsReachableAsync(Guid fromCodeId, Guid toCodeId, CancellationToken ct)
    {
        var visited = new HashSet<Guid> { fromCodeId };
        var q = new Queue<Guid>();
        q.Enqueue(fromCodeId);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            var next = await _policyRepo.GetAllowedNextAsync(cur, ct);

            foreach (var n in next)
            {
                if (n == toCodeId) return true;
                if (visited.Add(n)) q.Enqueue(n);
            }
        }
        return false;
    }

    private static string NormalizeCode(string? code) => (code ?? "").Trim().ToUpperInvariant();
    private static string? TrimOrNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}