//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimeSheetAggregate
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

public interface ITimesheetRepository
{
    Task<IReadOnlyList<TimesheetAggregate>> GetAllPersonsAsync(CancellationToken cancellationToken);

    Task<TimesheetAggregate?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken);
}
