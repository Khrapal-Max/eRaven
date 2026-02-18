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
/// Репозиторій табеля (episode root: <see cref="TimeSheetAggregate"/>) для операцій,
/// що синхронізують факти задач (TaskSpans) з документом бойових завдань.
///
/// <para>
/// Спрощена модель: документ одразу формує факт у табелі; факт не видаляємо — лише
/// компенсація (Cancel/Void).
/// </para>
/// </summary>
public interface ITimesheetAggregateRepository
{
    /// <summary>
    /// Застосовує/оновлює факти задач (TaskSpans) для конкретної місії документа
    /// на основі <see cref="CombatTaskDetails"/> (Start/End).
    /// </summary>
    Task ApplyCombatTaskFactsAsync(
        Guid documentId,
        Guid missionId,
        IReadOnlyCollection<CombatTaskDetails> details,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Компенсація (Cancel/Void) для фактів задач, створених/закритих цим документом.
    /// </summary>
    Task CancelCombatTaskFactsAsync(
        Guid documentId,
        Guid missionId,
        Guid reasonCodeId,
        string? reference,
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
