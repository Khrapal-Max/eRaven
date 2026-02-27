//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultQueryHandlers
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Dashboard;
using eRaven.Application.DTOs.Excel;
using eRaven.Application.DTOs.Person;
using eRaven.Application.DTOs.Timesheets;
using eRaven.Application.DTOs.Timesheets.Policy;
using eRaven.Application.Handlers.Dashboard;
using eRaven.Application.Handlers.Personal;
using eRaven.Application.Handlers.Timesheets;
using eRaven.Application.Queries;
using eRaven.Application.Queries.Dashboard;
using eRaven.Application.Queries.Personal;
using eRaven.Application.Queries.Timesheets;

namespace eRaven.Extensions;

public static class DefaultQueryHandlers
{
    public static IServiceCollection AddRegistredQueryHandlers(this IServiceCollection services)
    {
        // Persons
        services.AddScoped<IQueryHandler<GetPersonsPageQuery, PagedResult<PersonListItemDto>>, GetPersonsPageQueryHandler>();
        services.AddScoped<IQueryHandler<GetPersonDetailsQuery, PersonDetailsDto?>, GetPersonDetailsQueryHandler>();
        services.AddScoped<IQueryHandler<GetPersonHistoryQuery, IReadOnlyList<PersonEventListItemDto>>, GetPersonHistoryQueryHandler>();

        // Dashboard
        services.AddScoped<IQueryHandler<GetPersonnelDashboardQuery, PersonnelDashboardDto>, GetPersonnelDashboardQueryHandler>();

        // Polisy
        services.AddScoped<IQueryHandler<GetTimesheetPolicyForCodeQuery, TimesheetPolicyEditorDto?>, GetTimesheetPolicyForCodeQueryHandler>();
        services.AddScoped<IQueryHandler<GetTimesheetPolicyCodesQuery, IReadOnlyList<TimesheetCodeDto>>, GetTimesheetPolicyCodesQueryHandler>();
        services.AddScoped<IQueryHandler<GetTimesheetTransitionContextQuery, TimesheetTransitionContextDto>, GetTimesheetTransitionContextQueryHandler>();

        // Timesheets
        services.AddScoped<IQueryHandler<ExportTimesheetMonthQuery, DownloadFileDto>, ExportTimesheetMonthQueryHandler>();
        services.AddScoped<IQueryHandler<GetTimesheetPersonMonthQuery, TimesheetPersonMonthDto?>, GetTimesheetPersonMonthQueryHandler>();

        services.AddScoped<IQueryHandler<GetTimesheetsRangeQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>, GetTimesheetsRangeQueryHandler>();
        services.AddScoped<IQueryHandler<GetTimesheetsMonthQuery, IReadOnlyList<TimesheetPersonRangeRowDto>>, GetTimesheetsMonthQueryHandler>();

        return services;
    }
}
