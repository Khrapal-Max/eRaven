//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// TimesheetEntryRepositoryTests
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Тести для <see cref="TimesheetEntryRepository"/>.
///
/// <para>
/// Фіксуємо контракт CRUD/transition для <see cref="TimesheetEntry"/>:
/// <list type="bullet">
/// <item><description>read-методи ігнорують soft-delete;</description></item>
/// <item><description>overlap для entry інклюзивний: [From..To], To==null => open-ended;</description></item>
/// <item><description>GetActiveEntryOnDateAsync включає TimesheetCodeDefinition і повертає “найсвіжіший” запис;</description></item>
/// <item><description>Update/Transition заборонені у закритому епізоді;</description></item>
/// <item><description>Update/Transition валідують межі епізоду (OpenedAt..ClosedAt).</description></item>
/// </list>
/// </para>
/// </summary>
public sealed class TimesheetEntryRepositoryTests
{

}