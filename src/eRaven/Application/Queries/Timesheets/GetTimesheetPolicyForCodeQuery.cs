//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// GetTimesheetPolicyForCodeQuery
//-----------------------------------------------------------------------------

namespace eRaven.Application.Queries.Timesheets;

/// <summary>
/// Запит для отримання дозвільних кодів закріплених за кодом.
/// </summary>
public sealed record GetTimesheetPolicyForCodeQuery(Guid CodeId);
