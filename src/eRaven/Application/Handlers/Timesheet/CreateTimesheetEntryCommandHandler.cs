//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// CreateTimesheetEntryCommandHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Commands;
using eRaven.Application.Commands.Timesheet;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Application.Handlers.Timesheet;

public sealed class CreateTimesheetEntryCommandHandler(
    ITimesheetEntryRepository repo,
    ITimesheetPolicyRepository policyRepo)
    : ICommandHandler<CreateTimesheetEntryCommand>
{
    private readonly ITimesheetEntryRepository _repo = repo;
    private readonly ITimesheetPolicyRepository _policyRepo = policyRepo;

    public async Task HandleAsync(CreateTimesheetEntryCommand command, CancellationToken ct = default)
    {
        // 1) anchor: active entry on From (gives TimelineId + previous code)
        var prev = await _repo.GetActiveEntryOnDateAsync(command.PersonId, command.Lane, command.From, ct)
            ?? throw new InvalidOperationException("Не можна створити подію, так як нема історії.");

        // 2) load policy codes once (need ids + end-date meaning)
        var defs = await _policyRepo.GetCodesAsync(command.Lane, ct);

        var prevCodeNorm = NormalizeCode(prev.Code);
        var newCodeNorm = NormalizeCode(command.Code);

        var prevDef = defs.FirstOrDefault(x => NormalizeCode(x.Code) == prevCodeNorm)
            ?? throw new InvalidOperationException($"Policy: не знайдено код '{prevCodeNorm}' для lane {command.Lane}.");

        var newDef = defs.FirstOrDefault(x => NormalizeCode(x.Code) == newCodeNorm)
            ?? throw new InvalidOperationException($"Policy: не знайдено код '{newCodeNorm}' для lane {command.Lane}.");

        // 3) policy graph rule: must be reachable prev => new (без мостів у БД)
        // same-code allowed (0-length path)
        if (prevDef.Id != newDef.Id)
        {
            var ok = await IsReachableAsync(prevDef.Id, newDef.Id, ct);
            if (!ok)
                throw new InvalidOperationException($"Перехід '{prevCodeNorm}' до '{newCodeNorm}' заборонений політикою.");
        }

        var timelineId = prev.TimelineId;
        var prevCodeForReturn = prev.Code?.Trim() ?? prevCodeNorm;

        // 4) close previous at From-1
        prev.To = command.From.AddDays(-1);
        prev.UpdatedBy = command.Author;
        prev.UpdatedAtUtc = command.NowUtc;
        await _repo.UpdateAsync(prev, ct);

        // 5) normalize To for THIS code using end-date meaning (±1 day)
        DateOnly? newTo = command.To;

        if (command.To is not null && newDef.EndDateMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode)
            newTo = command.To.Value.AddDays(-1);

        // 6) add new entry
        await _repo.AddAsync(new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimelineId = timelineId,
            PersonId = command.PersonId,
            Lane = command.Lane,
            Code = newCodeNorm, // нормалізуємо, щоб не було " 30 "
            From = command.From,
            To = newTo,
            Reference = command.Reference,
            Note = command.Note,
            CreatedBy = command.Author,
            CreatedAtUtc = command.NowUtc
        }, ct);

        // 7) OPTIONAL: add "return" entry when user provided an end date
        if (command.To is not null)
        {
            if (newDef.EndDateMeaning == TimesheetEndDateMeaning.LastDayOfThisCode)
            {
                // return to previous code from the next day after the user's last day
                await _repo.AddAsync(new TimesheetEntry
                {
                    Id = Guid.NewGuid(),
                    TimelineId = timelineId,
                    PersonId = command.PersonId,
                    Lane = command.Lane,
                    Code = NormalizeCode(prevCodeForReturn),
                    From = command.To.Value.AddDays(1),
                    To = null,
                    CreatedBy = command.Author,
                    CreatedAtUtc = command.NowUtc
                }, ct);
            }
            else // FirstDayOfNextCode
            {
                // return date is exactly command.To (first day of next/base code)
                var next = string.IsNullOrWhiteSpace(newDef.NextCodeOnEnd) ? null : NormalizeCode(newDef.NextCodeOnEnd);
                if (!string.IsNullOrWhiteSpace(next))
                {
                    await _repo.AddAsync(new TimesheetEntry
                    {
                        Id = Guid.NewGuid(),
                        TimelineId = timelineId,
                        PersonId = command.PersonId,
                        Lane = command.Lane,
                        Code = next!,
                        From = command.To.Value,
                        To = null,
                        CreatedBy = command.Author,
                        CreatedAtUtc = command.NowUtc
                    }, ct);
                }
            }
        }
    }

    private async Task<bool> IsReachableAsync(Guid fromCodeId, Guid toCodeId, CancellationToken ct)
    {
        var visited = new HashSet<Guid>();
        var q = new Queue<Guid>();

        visited.Add(fromCodeId);
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

    private static string NormalizeCode(string? code)
        => (code ?? "").Trim().ToUpperInvariant();
}