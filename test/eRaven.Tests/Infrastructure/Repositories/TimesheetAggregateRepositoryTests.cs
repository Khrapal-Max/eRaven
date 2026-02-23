//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Infrastructure;
using eRaven.Tests.Extensions;
using Microsoft.EntityFrameworkCore;

namespace eRaven.Tests.Infrastructure.Repositories;

/// <summary>
/// Integration tests for <see cref="TimesheetAggregateRepository"/> task-control sync:
/// CombatTaskDetails -> MissionAssignment -> Timesheet entries (30/100).
/// </summary>

public sealed class TimesheetAggregateRepository_TaskControl_Tests
{

}
