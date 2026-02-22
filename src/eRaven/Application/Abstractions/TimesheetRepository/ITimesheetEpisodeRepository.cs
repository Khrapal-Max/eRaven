//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetEpisodeRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Репозиторій епізодів табеля (<see cref="TimeSheetAggregate"/>):
///
/// <para>Об'єднує:</para>
/// <list type="bullet">
/// <item><description>lifecycle-операції (Open/Close епізоду)</description></item>
/// <item><description>доступ до активного/актуального епізоду (read, інколи tracked)</description></item>
/// </list>
///
/// <para>Нотатки:</para>
/// <list type="bullet">
/// <item><description>Епізод — це період "в табелі" (OpenedAt..ClosedAt).</description></item>
/// <item><description>Системний стан <c>НБ</c> — derived (відсутній активний entry), не подія.</description></item>
/// </list>
/// </summary>
public interface ITimesheetEpisodeRepository
{
    /// <summary>
    /// Повертає епізод, активний на вказану дату (no-tracking).
    /// </summary>
    Task<TimeSheetAggregate?> GetEpisodeOnDateAsync(
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає активний (не закритий) епізод (no-tracking).
    /// </summary>
    Task<TimeSheetAggregate?> GetActiveEpisodeAsync(
        Guid personId,
        CancellationToken ct = default);

    /// <summary>
    /// Завантажує епізод, активний на дату, у tracked-режимі для команд/інваріантів.
    ///
    /// <para>Очікування реалізації:</para>
    /// <list type="bullet">
    /// </list>
    /// </summary>
    Task<TimeSheetAggregate?> LoadEpisodeOnDateForUpdateAsync(
        Guid personId,
        DateOnly date,
        CancellationToken ct = default);

    /// <summary>
    /// Відкриває епізод табеля при зарахуванні (ідемпотентно).
    /// </summary>
    Task OpenOnEnrollAsync(
        Guid personId,
        DateOnly enrollDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Перевіряє, чи можна закрити табель при виключенні на дату <paramref name="closeTo"/>.
    /// </summary>
    Task ValidateCanCloseOnExcludeAsync(
        Guid personId,
        DateOnly closeTo,
        CancellationToken ct = default);

    /// <summary>
    /// Закриває активний епізод табеля при виключенні.
    /// </summary>
    Task CloseOnExcludeAsync(
        Guid personId,
        DateOnly closeTo,
        string? reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
