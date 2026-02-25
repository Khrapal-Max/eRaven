//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultRepository
//-----------------------------------------------------------------------------

using eRaven.Application.Abstractions.DashboardRepository;
using eRaven.Application.Abstractions.MissionRepository;
using eRaven.Application.Abstractions.PersonRepository;
using eRaven.Application.Abstractions.TimesheetPolicyRepository;
using eRaven.Infrastructure.Repositories.DashboardRepository;
using eRaven.Infrastructure.Repositories.MissionRepository;
using eRaven.Infrastructure.Repositories.PersonRepository;
using eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

namespace eRaven.Extensions;

public static class DefaultRepository
{
    /// <summary>
    /// Реєструє репозиторії Infrastructure в DI контейнері.
    /// </summary>
    public static IServiceCollection AddRegistredRepositories(this IServiceCollection services)
    {
        // Dashboard - information about state persons on by cards
        services.AddScoped<IDashboardRepository, DashboardRepository>();

        // Persons cards
        services.AddScoped<IPersonRepository, PersonRepository>();

        // Policy
        services.AddScoped<ITimesheetPolicyRepository, TimesheetPolicyRepository>();

        // Timesheet (episode write + view read)

        // Missions
        services.AddScoped<IMissionRepository, MissionRepository>();

        return services;
    }
}
