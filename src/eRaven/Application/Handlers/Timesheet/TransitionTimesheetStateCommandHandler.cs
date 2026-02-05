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

/// <remarks>
/// Handler відповідає за:
/// - перевірку вхідних параметрів,
/// - policy-driven валідацію (дозволені переходи, required fields),
/// - розрахунок дат (prevLastDay/nextFrom),
/// - формування prev/next.
/// 
/// Інваріанти таймлайну (p.1/p.2):
/// - заборона вставок у закритий таймлайн,
/// - заборона виходу записів за межі таймлайну,
/// гарантуються репозиторієм write-шару.
/// </remarks>
public sealed class TransitionTimesheetStateCommandHandler(
    ITimesheetEntryRepository repo,
    ITimesheetPolicyRepository policyRepo,
    ITimesheetTimelineRepository timelines)
    : ICommandHandler<TransitionTimesheetStateCommand>
{
    private readonly ITimesheetEntryRepository _repo = repo;
    private readonly ITimesheetPolicyRepository _policyRepo = policyRepo;
    private readonly ITimesheetTimelineRepository _timelines = timelines;

    public async Task HandleAsync(TransitionTimesheetStateCommand command, CancellationToken ct = default)
    {
        if (command.PersonId == Guid.Empty)
            throw new ArgumentException("PersonId is required.", nameof(command.PersonId));

        ArgumentException.ThrowIfNullOrWhiteSpace(command.NextCode, nameof(command.NextCode));

        var author = string.IsNullOrWhiteSpace(command.Author) ? "system" : command.Author.Trim();
        var nextCodeNorm = NormalizeCode(command.NextCode);

        var timeline = await _timelines.GetTimelineOnDateAsync(command.PersonId, command.AnchorDate, ct)
            ?? throw new InvalidOperationException(
                "Не можна застосувати перехід: дата поза табелем (немає таймлайну на цю дату).");

        // Спрощена дата-валідація: input має бути в межах діючого timeline (мінімум — OpenedAt).
        if (command.InputDate < timeline.OpenedAt)
            throw new InvalidOperationException(
                $"Дата не може бути раніше відкриття табеля ({timeline.OpenedAt:yyyy-MM-dd}).");

        var prev = await _repo.GetActiveEntryOnDateAsync(timeline.Id, command.PersonId, command.AnchorDate, ct)
            ?? throw new InvalidOperationException(
                "Не можна застосувати перехід: на цю дату немає активної події (дані пошкоджені).");

        var prevCodeNorm = NormalizeCode(prev.Code);

        var defs = await _policyRepo.GetCodesAsync(ct);

        var prevDef = defs.FirstOrDefault(x => NormalizeCode(x.Code) == prevCodeNorm)
            ?? throw new InvalidOperationException($"Policy: не знайдено код '{prevCodeNorm}' у довіднику.");

        var nextDef = defs.FirstOrDefault(x => NormalizeCode(x.Code) == nextCodeNorm)
            ?? throw new InvalidOperationException($"Policy: не знайдено код '{nextCodeNorm}' у довіднику.");

        if (prevDef.Id == nextDef.Id)
            throw new InvalidOperationException($"Перехід у той самий код '{nextCodeNorm}' не має сенсу.");

        var ok = await IsReachableAsync(prevDef.Id, nextDef.Id, ct);
        if (!ok)
            throw new InvalidOperationException($"Перехід '{prevCodeNorm}' до '{nextCodeNorm}' заборонений політикою.");

        if (nextDef.RequiresReference && string.IsNullOrWhiteSpace(command.Reference))
            throw new InvalidOperationException($"Для коду '{nextCodeNorm}' обов'язково заповнити Reference.");

        if (nextDef.RequiresNote && string.IsNullOrWhiteSpace(command.Note))
            throw new InvalidOperationException($"Для коду '{nextCodeNorm}' обов'язково заповнити Note.");

        DateOnly prevLastDay;
        DateOnly nextFrom;

        if (prevDef.EndDateMeaning == TimesheetEndDateMeaning.LastDayOfThisCode)
        {
            prevLastDay = command.InputDate;
            nextFrom = command.InputDate.AddDays(1);
        }
        else
        {
            nextFrom = command.InputDate;
            prevLastDay = command.InputDate.AddDays(-1);
        }

        if (prevLastDay < prev.From)
            throw new InvalidOperationException(
                "Неможливо: дата завершення поточного стану раніше початку поточної події.");

        prev.To = prevLastDay;
        prev.UpdatedBy = author;
        prev.UpdatedAtUtc = command.NowUtc;

        var next = new TimesheetEntry
        {
            Id = Guid.NewGuid(),
            TimelineId = prev.TimelineId,
            PersonId = command.PersonId,
            Code = nextCodeNorm,
            From = nextFrom,
            To = null,
            Reference = TrimOrNull(command.Reference),
            Note = TrimOrNull(command.Note),
            CreatedBy = author,
            CreatedAtUtc = command.NowUtc,
            IsDeleted = false
        };

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