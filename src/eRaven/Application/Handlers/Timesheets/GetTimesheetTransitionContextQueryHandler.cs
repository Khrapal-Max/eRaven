//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// GetTimesheetTransitionContextQueryHandler
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Application.Abstractions.TimesheetRepository;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Mapper;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Timesheets;
using eRaven.Domain.Consts;
using eRaven.Domain.Enums;

namespace eRaven.Application.Handlers.Timesheets;

/// <summary>
/// Формує контекст переходу табеля для UI на конкретну дату:
/// поточний код + дозволені опції переходів (матриця) + emergency (глобально).
/// </summary>
public sealed class GetTimesheetTransitionContextQueryHandler(
    ITimesheetEntryQueryRepository entries,
    ITimesheetPolicyRepository policy)
    : IQueryHandler<GetTimesheetTransitionContextQuery, TimesheetTransitionContextDto>
{
    private readonly ITimesheetEntryQueryRepository _entries = entries;
    private readonly ITimesheetPolicyRepository _policy = policy;

    public async Task<TimesheetTransitionContextDto> HandleAsync(
        GetTimesheetTransitionContextQuery query,
        CancellationToken ct = default)
    {
        if (query.PersonId == Guid.Empty)
            throw new InvalidOperationException("PersonId обов'язковий.");
        if (query.OnDate == default)
            throw new InvalidOperationException("OnDate обов'язковий.");

        // 1) Поточний entry на дату (може бути null => derived NB)
        var entry = await _entries.GetActiveEntryOnDateAsync(query.PersonId, query.OnDate, ct);

        if (entry?.TimesheetCodeDefinition is null)
        {
            return new TimesheetTransitionContextDto(
                PersonId: query.PersonId,
                OnDate: query.OnDate,
                CurrentCodeId: null,
                CurrentCode: TimesheetDerivedCodes.NotInTimesheet,
                IsDerived: true,
                CurrentRole: null,
                TransitionOptions: [],
                EmergencyOptions: []);
        }

        var currentCodeId = entry.TimesheetCodeDefinitionId;
        var currentCode = (entry.TimesheetCodeDefinition.Code ?? string.Empty).Trim();
        var currentRole = entry.TimesheetCodeDefinition.RoleCode;

        // derived не може сюди потрапити, але захистимося
        if (string.IsNullOrWhiteSpace(currentCode))
            currentCode = TimesheetDerivedCodes.NotInTimesheet;

        // 2) Emergency options — усі EmergencyCode (глобально), shift=0
        var allCodes = await _policy.GetCodesAsync(includeInactive: false, ct);
        var emergency = allCodes
            .Where(c => c.IsActive && c.RoleCode == RoleCode.EmergencyCode)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Priority).ThenBy(c => c.Code)
            .Select(c => new TimesheetTransitionOptionDto(
                TransitionCodeId: c.Id,
                Code: (c.Code ?? string.Empty).Trim(),
                Display: (c.Title ?? string.Empty).Trim(),
                StartShiftDays: 0))
            .ToList();

        // 3) derived/system current => transition options empty
        if (currentRole == RoleCode.SystemCode)
        {
            return new TimesheetTransitionContextDto(
                PersonId: query.PersonId,
                OnDate: query.OnDate,
                CurrentCodeId: currentCodeId,
                CurrentCode: currentCode,
                IsDerived: false,
                CurrentRole: TimesheetEnumMapper.MapRole(currentRole),
                TransitionOptions: [],
                EmergencyOptions: emergency);
        }

        // 4) Transition options by matrix (тільки TransitionCode)
        var rules = await _policy.GetAllowedCodesAsync(currentCodeId, ct);

        var transitionOptions = rules
            .Where(r => r.ToCode is not null && r.ToCode.RoleCode == RoleCode.TransitionCode)
            .Select(r => new TimesheetTransitionOptionDto(
                TransitionCodeId: r.ToCodeId,
                Code: (r.ToCode!.Code ?? string.Empty).Trim(),
                Display: (r.ToCode!.Title ?? string.Empty).Trim(),
                StartShiftDays: r.StartShiftDays))
            .OrderBy(o => o.Code)
            .ToList();

        return new TimesheetTransitionContextDto(
            PersonId: query.PersonId,
            OnDate: query.OnDate,
            CurrentCodeId: currentCodeId,
            CurrentCode: currentCode,
            IsDerived: false,
            CurrentRole: TimesheetEnumMapper.MapRole(currentRole),
            TransitionOptions: transitionOptions,
            EmergencyOptions: emergency);
    }
}
