//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// PlanActionService (lightweight guards)
//-----------------------------------------------------------------------------

using eRaven.Infrastructure;

namespace eRaven.Application.Services.PlanActionService;

public class PlanActionService(AppDbContext db) : IPlanActionService
{
    private readonly AppDbContext _db = db;


}