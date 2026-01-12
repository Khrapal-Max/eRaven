//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// PersonRepository
//-----------------------------------------------------------------------------

using eRaven.Infrastructure.Projectors;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Infrastructure.Repositories.PersonRepository;

public sealed class PersonRepository(
    IDbContextFactory<AppDbContext> dbFactory,
    IPersonReadModelProjector projector)
    : IPersonRepository
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory = dbFactory;
    private readonly IPersonReadModelProjector _projector = projector;


}