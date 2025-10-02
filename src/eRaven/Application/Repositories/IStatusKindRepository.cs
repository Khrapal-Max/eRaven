//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IStatusKindRepository Application layer
//-----------------------------------------------------------------------------

using eRaven.Domain.Models;

namespace eRaven.Application.Repositories;

public interface IStatusKindRepository
{
    StatusKind? GetById(int id);

    Task<IReadOnlyList<StatusKind>> GetAllAsync(CancellationToken ct = default);
}