//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetAggregateRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Write-репозиторій для <see cref="TimeSheetAggregate"/> (tracked).
///
/// <para>Призначення:</para>
/// <list type="bullet">
/// <item><description>завантажити епізод з навігаціями (<c>Entries</c>, <c>TaskSpans</c>) для змін;</description></item>
/// <item><description>зберегти зміни агрегату (<c>SaveChanges</c>).</description></item>
/// </list>
///
/// <para>Примітка:</para>
/// <list type="bullet">
/// <item><description>Read-запити для UI/звітів залишаються у Month/Range репозиторії.</description></item>
/// </list>
/// </summary>
public interface ITimesheetAggregateRepository
{
    /// <summary>
    /// Завантажує активний (ClosedAt == null) епізод для редагування.
    /// Повертає tracked aggregate з включеними <c>Entries</c> та <c>TaskSpans</c>.
    /// </summary>
    Task<TimeSheetAggregate?> LoadActiveForUpdateAsync(
        Guid personId,
        CancellationToken ct = default);

    /// <summary>
    /// Завантажує епізод, який покриває дату <paramref name="date"/>, для редагування.
    /// </summary>
    Task<TimeSheetAggregate?> LoadOnDateForUpdateAsync(
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Зберігає зміни агрегату (SaveChanges).
    /// </summary>
    Task SaveAsync(CancellationToken ct = default);
}
