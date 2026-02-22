//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// MissionRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Infrastructure.Repositories.MissionRepository;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="MissionRepository"/>.
///
/// <para>
/// Стратегія:
/// <list type="bullet">
/// <item><description>місію можна закрити лише якщо немає активного призначення (TimesheetTaskSpan) на дату закриття;</description></item>
/// <item><description>активність визначається як <c>Status != Canceled</c> та half-open інтервал <c>[FromDate..ToDate)</c>;</description></item>
/// <item><description>canceled-спани не блокують закриття місії.</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class MissionRepositoryTests
{


}
