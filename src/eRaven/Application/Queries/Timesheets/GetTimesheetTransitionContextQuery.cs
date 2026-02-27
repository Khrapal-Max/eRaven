//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetTransitionContextQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.Timesheets;

/// <summary>
/// Повертає контекст переходу табеля для особи на конкретну дату:
/// поточний код + дозволені переходи.
/// </summary>
public sealed record GetTimesheetTransitionContextQuery(
    Guid PersonId,
    DateOnly OnDate);
