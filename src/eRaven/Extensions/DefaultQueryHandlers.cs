//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultQueryHandlers
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Application.DTOs.Dashboard;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Mission;
using eRaven.Application.DTOs.Person;
using eRaven.Application.DTOs.Timesheet;
using eRaven.Application.Handlers.CombatTask;
using eRaven.Application.Handlers.Dashboard;
using eRaven.Application.Handlers.Mission;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Handlers.Timesheet;
using eRaven.Application.Queries;
using eRaven.Application.Queries.CombatTask;
using eRaven.Application.Queries.Dashboard;
using eRaven.Application.Queries.Mission;
using eRaven.Application.Queries.Personal;
using eRaven.Application.Queries.Timesheet;

namespace eRaven.Extensions;

public static class DefaultQueryHandlers
{
    public static IServiceCollection AddRegistredQueryHandlers(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>, GetPersonsPageQueryHandler>();
        services.AddScoped<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>, GetPersonDetailsQueryHandler>();
        services.AddScoped<IQueryHandler<GetPersonHistoryQuery, IReadOnlyList<PersonEventListItemDto>>, GetPersonHistoryQueryHandler>();
        services.AddScoped<IQueryHandler<GetPersonnelDashboardQuery, PersonnelDashboardDto>, GetPersonnelDashboardQueryHandler>();

        services.AddScoped<IQueryHandler<GetTimesheetPersonMonthQuery, TimesheetPersonMonthDto>, GetTimesheetPersonMonthQueryHandler>();
        services.AddScoped<IQueryHandler<GetTimesheetMonthQuery, IReadOnlyList<TimesheetPersonMonthRowDto>>, GetTimesheetMonthQueryHandler>();
        services.AddScoped<IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>, ExportTimesheetMonthQueryHandler>();
        services.AddScoped<IQueryHandler<GetTimesheetDayQuery, IReadOnlyList<TimesheetPersonDayRowDto>>, GetTimesheetDayQueryHandler>();

        services.AddScoped<IQueryHandler<GetTimesheetRangeQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>, GetTimesheetRangeQueryHandler>();

        services.AddScoped<IQueryHandler<GetMissionsQuery, IReadOnlyList<MissionDto>>, GetMissionsQueryHandler>();

        services.AddScoped<IQueryHandler<GetTimesheetPolicyCodesQuery, IReadOnlyList<TimesheetCodeDto>>, GetTimesheetPolicyCodesQueryHandler>();
        services.AddScoped<IQueryHandler<GetTimesheetPolicyForCodeQuery, TimesheetPolicyEditorDto?>, GetTimesheetPolicyForCodeQueryHandler>();
        services.AddScoped<IQueryHandler<GetTimesheetPolicyForCodeQuery, IReadOnlyList<TimesheetTransitionOptionDto>>, GetTimesheetPolicyForCodeOptionQueryHandler>();

        services.AddScoped<IQueryHandler<GetCombatTaskPersonLookupQuery, IReadOnlyList<ReadyCombatTaskPersonDto>>, GetCombatTaskPersonLookupQueryHandler>();
        services.AddScoped<IQueryHandler<GetCombatTaskDocumentsQuery, IReadOnlyList<CombatTaskDocumentDto>>, GetCombatTaskDocumentsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCombatTaskDetailsByDocumentIdQuery, CombatTaskEditorDto?>, GetCombatTaskDetailsByDocumentIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetCombatTaskMissionPersonsQuery, IReadOnlyList<ActiveMissionPersonDto>>, GetCombatTaskMissionPersonsQueryHandler>();
        services.AddScoped<IQueryHandler<GetCombatTaskPersonLookupQuery, IReadOnlyList<ReadyCombatTaskPersonDto>>, GetCombatTaskPersonLookupQueryHandler>();
        services.AddScoped<IQueryHandler<GetCombatTaskMissionPersonsQuery, IReadOnlyList<ActiveMissionPersonDto>>, GetCombatTaskMissionPersonsQueryHandler>();

        return services;
    }
}
