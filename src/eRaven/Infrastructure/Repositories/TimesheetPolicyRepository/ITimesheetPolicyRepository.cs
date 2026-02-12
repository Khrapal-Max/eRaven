//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetPolicyRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.Timesheet;
using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.TimesheetPolicyRepository;

/// <summary>
/// Репозиторій політик табеля (довідник кодів + дозволені переходи + трактовка дати події).
/// </summary>
public interface ITimesheetPolicyRepository
{
    // ----------------------------
    // Codes
    // ----------------------------

    /// <summary>
    /// Повертає коди табеля (за замовчуванням — тільки активні).
    /// Сортування: SortOrder → Priority → Code.
    /// </summary>
    Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    /// <summary>Повертає код за Id (tracked = false).</summary>
    Task<TimesheetCodeDefinition?> GetCodeByIdAsync(Guid codeId, CancellationToken ct = default);

    /// <summary>Створює новий код у довіднику.</summary>
    Task<Guid> AddCodeAsync(
        string code,
        string title,
        string? description,
        int sortOrder,
        int priority,
        bool isTerminal,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>Закриває код (робить неактивним). Не видаляє.</summary>
    Task CloseCodeAsync(
        Guid codeId,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    // ----------------------------
    // Rules (Transitions)
    // ----------------------------

    /// <summary>
    /// Повертає дозволені переходи для fromCodeId.
    /// </summary>
    Task<IReadOnlyList<TimesheetCodeTransition>> GetAllowedTransitionsAsync(
        Guid fromCodeId,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає дозволені переходи для code у вигляді DTO (для UI). 
    /// Повертає порожній список, якщо код не знайдено або неактивний.
    /// </summary>
    Task<IReadOnlyList<TimesheetTransitionOptionDto>> GetAllowedTransitionOptionsAsync(
        string code,
        CancellationToken ct = default);

    /// <summary>
    /// Зберігає зміни коду та його правила переходів (диф-оновлення без втрати даних).
    /// </summary>
    Task SavePolicyAsync(
        Guid codeId,
        string title,
        string? description,
        int sortOrder,
        int priority,
        bool isTerminal,
        IReadOnlyCollection<TimesheetTransitionSpecDto> allowedTransitions,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}