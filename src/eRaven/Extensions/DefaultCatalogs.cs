//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// DefaultCatalofs
//-----------------------------------------------------------------------------

using eRaven.Application.Catalogs.CombatTask.Targets;
using eRaven.Application.Catalogs.CombatTask.TypeDrones;
using eRaven.Application.Catalogs.Ranks;

namespace eRaven.Extensions;

public static class DefaultCatalogs
{
    public static IServiceCollection AddRegistredCatalogs(this IServiceCollection services)
    {
        services.AddScoped<IRankCatalog, DefaultRankCatalog>();
        services.AddScoped<ITypeDroneCatalog, DefaultTypeDroneCatalog>();
        services.AddScoped<ITargetCatalog, DefaultTargetCatalog>();

        return services;
    }
}
