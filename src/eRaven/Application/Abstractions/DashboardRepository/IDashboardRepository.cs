//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IDashboardRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Application.Abstractions.DashboardRepository;

/// <summary>
/// Повертає активні карти людей
/// </summary>
public interface IDashboardRepository
{
    /// <summary>
    /// Повертає список активних карток людей
    /// </summary>
    Task<IReadOnlyList<PersonReadModel>> GetPersonnelDashboardAsync(CancellationToken ct = default);
}
