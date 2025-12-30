//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IRankRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.RankRepository;

public interface IRankRepository
{
    Task<IEnumerable<Rank>> GetAllRanksAsync(CancellationToken ct);

    Task AddRankAsync(Rank rank, CancellationToken ct);

    Task DeActivatedRankAsync(Guid id, CancellationToken ct);

    Task<bool> ActiveTitleExistsAsync(string title, CancellationToken ct);
}