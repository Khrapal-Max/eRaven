//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IRankCatalog
//-----------------------------------------------------------------------------

namespace eRaven.Application.Catalogs.Ranks;

public interface IRankCatalog
{
    IReadOnlyList<RankOption> GetActive();
}
