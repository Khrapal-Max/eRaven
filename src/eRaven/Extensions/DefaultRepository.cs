//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultRepository
//-----------------------------------------------------------------------------

using eRaven.Infrastructure.Repositories.CombatTaskRepository;
using eRaven.Infrastructure.Repositories.DashboardRepository;
using eRaven.Infrastructure.Repositories.MissionRepository;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.TimesheetRepository;

namespace eRaven.Extensions;

public static class DefaultRepository
{
    public static IServiceCollection AddRegistredRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ITimesheetTimelineRepository, TimesheetTimelineRepository>();
        services.AddScoped<ITimesheetEntryRepository, TimesheetEntryRepository>();
        services.AddScoped<ITimesheetLifecycleRepository, TimesheetLifecycleRepository>();
        services.AddScoped<ITimesheetPolicyRepository, TimesheetPolicyRepository>();
        services.AddScoped<ITimesheetMonthRepository, TimesheetMonthRepository>();
        services.AddScoped<ICombatTaskDocumentRepository, CombatTaskDocumentRepository>();
        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<ICombatTaskEntryRepository, CombatTaskEntryRepository>();

        return services;
    }
}
