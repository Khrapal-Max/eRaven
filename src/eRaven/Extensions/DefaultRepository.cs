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
    /// <summary>
    /// Реєструє репозиторії Infrastructure в DI контейнері.
    /// </summary>
    public static IServiceCollection AddRegistredRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPersonRepository, PersonRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        // Timesheet (episode write + view read)
        services.AddScoped<ITimesheetPolicyRepository, TimesheetPolicyRepository>();
        services.AddScoped<ITimesheetEpisodeRepository, TimesheetEpisodeRepository>();
        services.AddScoped<ITimesheetEntryRepository, TimesheetEntryRepository>();
        services.AddScoped<ITimesheetViewRepository, TimesheetViewRepository>();
        services.AddScoped<ITimesheetMissionPlanningRepository, TimesheetMissionPlanningRepository>();

        // Mission/Combat tasks

        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<IMissionAssignmentRepository, MissionAssignmentRepository>();
        services.AddScoped<ICombatTaskRepository, CombatTaskRepository>();
        services.AddScoped<ICombatTaskDocumentRepository, CombatTaskDocumentRepository>();

        return services;
    }
}
