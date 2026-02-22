//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetAggregateRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Aggregates;
using eRaven.Domain.Entities;

namespace eRaven.Application.Abstractions.TimesheetRepository;

/// <summary>
/// Репозиторій табеля (episode root: <see cref="TimeSheetAggregate"/>).
/// 
/// <para>
/// Табель не "контролює" завдання. Джерело правди "хто/коли на завданні" — домен CombatTask.
/// Цей репозиторій виконує роль синхронізатора: матеріалізує зміни документа/призначень у табель
/// у вигляді подій (коди 30/100 з reference).
/// </para>
/// </summary>
public interface ITimesheetAggregateRepository
{
    /// <summary>
    /// Застосовує/оновлює факти завдання для конкретної місії документа
    /// на основі <see cref="CombatTaskDetails"/> (Start/End).
    /// 
    /// <para>
    /// Репозиторій:
    /// <list type="bullet">
    /// <item><description>оновлює матеріалізовані призначення (<see cref="MissionAssignment"/>);</description></item>
    /// <item><description>перераховує події табеля 30/100 для affected persons.</description></item>
    /// </list>
    /// </para>
    /// </summary>
    Task ApplyCombatTaskFactsAsync(
        Guid documentId,
        string documentOrderTitle,
        Guid missionId,
        IReadOnlyCollection<CombatTaskDetails> details,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Компенсація (Cancel/Void) для місії документа:
    /// видаляє призначення документа та перераховує події табеля.
    /// </summary>
    Task CancelCombatTaskFactsAsync(
        Guid documentId,
        Guid missionId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Завантажує активний епізод табеля (ClosedAt == null) для подальших змін.
    /// </summary>
    Task<TimeSheetAggregate> LoadActiveAsync(Guid personId, CancellationToken ct = default);

    /// <summary>
    /// Завантажує епізод табеля, який активний на дату <paramref name="onDate"/>, для подальших змін.
    /// </summary>
    Task<TimeSheetAggregate> LoadOnDateForUpdateAsync(Guid personId, DateOnly onDate, CancellationToken ct = default);

    /// <summary>
    /// Зберігає зміни в поточному DbContext, який використовується методами Load*.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
