//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetEpisodeRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Репозиторій епізодів табеля (<see cref="TimeSheetAggregate"/>): lifecycle (Open/Close) + read-доступ.
///
/// <para>
/// Епізод — це часовий відрізок життя табеля особи: <c>[OpenedAt..ClosedAt]</c> (ClosedAt — inclusive).
/// </para>
///
/// <para>
/// ВАЖЛИВО: цей контракт не віддає tracked-агрегат назовні.
/// Команди, які модифікують події, виконуються через репозиторій подій (writer),
/// який завантажує агрегат всередині транзакції.
/// </para>
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
    /// Відкриває епізод табеля при зарахуванні (ідемпотентно).
    /// <para>
    /// Реалізація повинна створити дефолтну подію на дату <paramref name="enrollDate"/>.
    /// </para>
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
    /// Закриває активний епізод табеля при виключенні (inclusive дата <paramref name="closeTo"/>).
    /// </summary>
    Task CloseOnExcludeAsync(
        Guid personId,
        DateOnly closeTo,
        string? reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
