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

        // 2) policy of NEW code (the one we add)
        var def = (await _policyRepo.GetCodesAsync(command.Lane, ct))
            .First(x => x.Code == command.Code);

        var timelineId = prev.TimelineId;
        var prevCode = prev.Code;

        // 3) close previous at From-1
        prev.To = command.From.AddDays(-1);
        prev.UpdatedBy = command.Author;
        prev.UpdatedAtUtc = command.NowUtc;
        await _repo.UpdateAsync(prev, ct);

        // 4) normalize To for THIS code using end-date meaning (±1 day)
        DateOnly? newTo = command.To;

        if (command.To is not null && def.EndDateMeaning == TimesheetEndDateMeaning.FirstDayOfNextCode)
            newTo = command.To.Value.AddDays(-1);

        // 5) add new entry
        await _repo.AddAsync(new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimelineId = timelineId,
            PersonId = command.PersonId,
            Lane = command.Lane,
            Code = command.Code,
            From = command.From,
            To = newTo,
            Reference = command.Reference,
            Note = command.Note,
            CreatedBy = command.Author,
            CreatedAtUtc = command.NowUtc
        }, ct);

        // 6) OPTIONAL: add "return" entry when user provided an end date
        if (command.To is not null)
        {
            if (def.EndDateMeaning == TimesheetEndDateMeaning.LastDayOfThisCode)
            {
                // return to previous code from the next day after the user's last day
                await _repo.AddAsync(new TimesheetEntry
                {
                    Id = Guid.NewGuid(),
                    TimelineId = timelineId,
                    PersonId = command.PersonId,
                    Lane = command.Lane,
                    Code = prevCode,
                    From = command.To.Value.AddDays(1),
                    To = null,
                    CreatedBy = command.Author,
                    CreatedAtUtc = command.NowUtc
                }, ct);
            }
            else // FirstDayOfNextCode
            {
                // return date is exactly command.To (first day of next/base code)
                if (!string.IsNullOrWhiteSpace(def.NextCodeOnEnd))
                {
                    await _repo.AddAsync(new TimesheetEntry
                    {
                        Id = Guid.NewGuid(),
                        TimelineId = timelineId,
                        PersonId = command.PersonId,
                        Lane = command.Lane,
                        Code = def.NextCodeOnEnd.Trim(),
                        From = command.To.Value,
                        To = null,
                        CreatedBy = command.Author,
                        CreatedAtUtc = command.NowUtc
                    }, ct);
                }
            }
        }
    }
}
