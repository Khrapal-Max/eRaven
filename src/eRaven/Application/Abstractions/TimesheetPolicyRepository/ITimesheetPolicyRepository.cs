//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetPolicyRepository
//-----------------------------------------------------------------------------
//
// Контракт для керування:
// 1) довідником табельних кодів (TimesheetCodeDefinition)
// 2) суворою матрицею переходів (TimesheetCodeTransition)
//
// Політика табеля (Feb 2026):
// - "НБ" (TimesheetDerivedCodes.NotInTimesheet) — derived gap, НЕ є реальним кодом у довіднику.
// - SystemCode створюється/оновлюється лише системою (не вручну).
// - Матриця переходів (policy) застосовується тільки для TransitionCode → TransitionCode.
// - EmergencyCode — глобальні опції.
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;
using eRaven.Domain.Enums;
using eRaven.Domain.ValueObjects;

namespace eRaven.Application.Abstractions.TimesheetPolicyRepository;

/// <summary>
/// Репозиторій політики табеля: коди + правила переходів.
/// </summary>
public interface ITimesheetPolicyRepository
{
    //======================================================================
    // Reads
    //======================================================================

    /// <summary>
    /// Повертає довідник кодів. Якщо <paramref name="includeInactive"/> = true — додає неактивні.
    /// NB ("НБ") завжди виключається (derived).
    /// </summary>
    Task<IReadOnlyList<TimesheetCodeDefinition>> GetCodesAsync(bool includeInactive, CancellationToken ct = default);

    /// <summary>
    /// Повертає всі дозволені коди для поточного коду <paramref name="fromCodeId"/>:
    /// транзитні (за strict-матрицею) + аварійні (глобальні EmergencyCode).
    /// </summary>
    Task<IReadOnlyList<TimesheetCodeTransition>> GetAllowedCodesAsync(Guid fromCodeId, CancellationToken ct = default);

    /// <summary>
    /// Повертає транзитні цілі (TransitionCode) з матриці для коду <paramref name="fromCodeId"/>.
    /// Використовується в редакторі політики.
    /// </summary>
    Task<IReadOnlyList<TimesheetCodeTransition>> GetTransitionCodesAsync(Guid fromCodeId, CancellationToken ct = default);

    /// <summary>
    /// Глобальні аварійні коди (EmergencyCode), незалежно від поточного стану.
    /// </summary>
    Task<IReadOnlyList<TimesheetCodeDefinition>> GetEmergencyCodesAsync(CancellationToken ct = default);

    /// <summary>
    /// Повертає код за Id (тільки active). Повертає null для неактивних/derived.
    /// </summary>
    Task<TimesheetCodeDefinition?> GetCodeByIdAsync(Guid codeId, CancellationToken ct = default);

    //======================================================================
    // Writes
    //======================================================================

    /// <summary>
    /// Створює табельний код (не допускає SystemCode та derived "НБ").
    /// </summary>
    Task<Guid> AddCodeAsync(
        string code,
        string title,
        string? description,
        int sortOrder,
        int priority,
        bool isTerminal,
        RoleCode roleCode,
        TimesheetUiStyle uiStyle,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Робить код неактивним (idempotent). SystemCode/derived "НБ" заборонені до закриття.
    /// </summary>
    Task CloseCodeAsync(Guid codeId, string author, DateTime nowUtc, CancellationToken ct = default);

    /// <summary>
    /// Оновлює атрибути коду та зберігає його матрицю переходів (diff-update).
    /// </summary>
    Task SavePolicyAsync(
        Guid codeId,
        string title,
        string? description,
        int sortOrder,
        int priority,
        bool isTerminal,
        IReadOnlyCollection<TimesheetTransitionSpec> allowedTransitions,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
