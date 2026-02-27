//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPersonMonthQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.Mapper;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Query handler: повертає персональний табель за місяць (матриця + entries).
/// </summary>
public sealed class GetTimesheetPersonMonthQueryHandler(
    ITimesheetViewRepository viewRepo,
    ITimesheetEntryQueryRepository entryQueryRepo,
    ITimesheetPolicyRepository policyRepo,
    IPersonRepository persons)
    : IQueryHandler<GetTimesheetPersonMonthQuery, TimesheetPersonMonthDto?>
{
    private readonly ITimesheetViewRepository _viewRepo = viewRepo;
    private readonly ITimesheetEntryQueryRepository _entryQueryRepo = entryQueryRepo;
    private readonly ITimesheetPolicyRepository _policyRepo = policyRepo;
    private readonly IPersonRepository _personsRepo = persons;

    public async Task<TimesheetPersonMonthDto?> HandleAsync(
        GetTimesheetPersonMonthQuery query,
        CancellationToken ct = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Year, 2000, nameof(query.Year));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Year, 2100, nameof(query.Year));
        ArgumentOutOfRangeException.ThrowIfLessThan(query.Month, 1, nameof(query.Month));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(query.Month, 12, nameof(query.Month));

        var period = await _viewRepo.GetTimesheetPersonMonthAsync(query.PersonId, query.Year, query.Month, ct);
        if (period is null) return null;

        var person = await _personsRepo.GetByIdAsync(query.PersonId, ct);
        if (person is null) return null;

        var fromDate = new DateOnly(query.Year, query.Month, 1);
        var daysInMonth = DateTime.DaysInMonth(query.Year, query.Month);
        var toDate = fromDate.AddDays(daysInMonth - 1);

        // source of truth
        var entries = await _entryQueryRepo.GetEntriesForPersonAsync(query.PersonId, fromDate, toDate, ct);

        List<TimesheetPersonEntryRowDto> entryDtos;
        DateTime updatedAtUtc;

        if (entries.Count == 0)
        {
            entryDtos = [];
            updatedAtUtc = DateTime.MinValue;
        }
        else
        {
            // code dictionary (for entry list)
            var defs = await _policyRepo.GetCodesAsync(includeInactive: true, ct);
            var codeById = defs.ToDictionary(x => x.Id, x => (x.Code ?? string.Empty).Trim());

            entryDtos = [.. entries
                .OrderBy(x => x.From)
                .ThenBy(x => x.Id)
                .Select(x => new TimesheetPersonEntryRowDto(
                    Code: codeById.TryGetValue(x.TimesheetCodeDefinitionId, out var c) ? c : string.Empty,
                    From: x.From,
                    To: x.To,
                    Reference: string.IsNullOrWhiteSpace(x.Reference) ? null : x.Reference.Trim(),
                    Note: string.IsNullOrWhiteSpace(x.Note) ? null : x.Note.Trim()))];

            updatedAtUtc = entries.Max(x => x.UpdatedAtUtc ?? x.CreatedAtUtc);
        }

        var personDto = TimesheetDtoMapper.MapPerson(person);
        var dayDtos = period.Days.Select(TimesheetDtoMapper.MapDay).ToList();

        return new TimesheetPersonMonthDto(
            Person: personDto,
            UpdatedAtUtc: updatedAtUtc,
            Days: dayDtos,
            Entries: entryDtos);
    }
}
