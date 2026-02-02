//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetPolicyRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

/// <summary>
/// Репозиторій політик табеля (довідник кодів + дозволені переходи).
///
/// Джерела істини:
/// - <see cref="TimesheetCodeDefinition"/> — опис коду (назва, правила завершення, прапорці).
/// - <see cref="TimesheetCodeTransition"/> — дозволені переходи (FromCodeId → ToCodeId).
///
/// Примітка:
/// - Lane прибрано, тому політика застосовується глобально.
/// </summary>
public interface ITimesheetPolicyRepository
{
    /// <summary>
    /// Повертає всі активні коди табеля для UI.
    /// Сортування: <see cref="TimesheetCodeDefinition.SortOrder"/> → <see cref="TimesheetCodeDefinition.Code"/>.
    /// </summary>
    Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(CancellationToken ct = default);

    /// <summary>
    /// Повертає множину Id кодів, які дозволені як "наступні" для <paramref name="fromCodeId"/>.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetAllowedNextAsync(Guid fromCodeId, CancellationToken ct = default);

    /// <summary>
    /// Зберігає політику для одного коду та повністю переписує список дозволених переходів.
    ///
    /// Оновлює властивості коду:
    /// - <see cref="TimesheetCodeDefinition.EndDateMeaning"/>
    /// - <see cref="TimesheetCodeDefinition.NextCodeOnEnd"/>
    ///
    /// Перезаписує transitions:
    /// - видаляє всі наявні переходи для <paramref name="fromCodeId"/>
    /// - додає нові переходи згідно <paramref name="allowedToCodeIds"/>
    ///
    /// Правила:
    /// - Якщо <paramref name="endDateMeaning"/> == <see cref="TimesheetEndDateMeaning.FirstDayOfNextCode"/>:
    ///   - <paramref name="nextCodeOnEnd"/> якщо пустий → дефолт "30"
    ///   - nextCodeOnEnd має існувати серед активних кодів, інакше помилка
    /// - Якщо <paramref name="endDateMeaning"/> == <see cref="TimesheetEndDateMeaning.LastDayOfThisCode"/>:
    ///   - nextCodeOnEnd примусово стає null
    /// - <paramref name="allowedToCodeIds"/>:
    ///   - ігноруємо Guid.Empty та самого себе (fromCodeId)
    ///   - всі коди мають існувати та бути активними, інакше помилка
    /// </summary>
    Task SavePolicyAsync(
        Guid fromCodeId,
        TimesheetEndDateMeaning endDateMeaning,
        string? nextCodeOnEnd,
        IReadOnlyCollection<Guid> allowedToCodeIds,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
