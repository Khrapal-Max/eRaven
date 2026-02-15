//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetLifecycleRepository
//-----------------------------------------------------------------------------

using eRaven.Domain.Entities;

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Репозиторій життєвого циклу табеля (епізоди зарахування/виключення).
///
/// Модель:
/// - Кожне зарахування (Enroll) створює НОВИЙ <see cref="eRaven.Domain.Aggregates.TimeSheetAggregate"/> (епізод),
///   якщо немає активного епізоду.
/// - Старі (закриті) таймлайни НЕ перезаписуються і не "перевідкриваються".
/// - Одночасно дозволено мати не більше одного активного таймлайну (ClosedAt == null).
///
/// Важливо:
/// - "НБ" не зберігаємо як entry — це derived-стан, коли немає активного запису на дату.
/// - Діапазони інклюзивні: [From..To]. Якщо To == null — запис відкритий у майбутнє (лише для активного епізоду).
/// </summary>
public interface ITimesheetLifecycleRepository
{
    /// <summary>
    /// Відкриває табель при зарахуванні особи (створює епізод).
    ///
    /// Правила:
    /// - Якщо активного епізоду немає — створюється новий <see cref="eRaven.Domain.Aggregates.TimeSheetAggregate"/> з OpenedAt=enrollDate
    ///   і додається дефолтний запис з кодом "Т", який покриває enrollDate.
    /// - Якщо активний епізод є — операція має бути ідемпотентною (не створює дублікати),
    ///   але НЕ змінює OpenedAt і НЕ створює новий епізод поверх старого.
    /// - Заборонено відкривати новий епізод "у минулому", який перетинається/накладається на закриті епізоди.
    /// </summary>
    Task OpenOnEnrollAsync(
        Guid personId,
        DateOnly enrollDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Перевіряє (без внесення змін), чи дозволено закривати активний епізод на дату <paramref name="closeTo"/>.
    ///
    /// Правило:
    /// - На дату closeTo активний запис має бути у дозволеному стані
    ///   (наприклад, "Т" або "РОЗПОР" — визначається реалізацією).
    /// </summary>
    Task ValidateCanCloseOnExcludeAsync(
        Guid personId,
        DateOnly closeTo,
        CancellationToken ct = default);

    /// <summary>
    /// Закриває активний епізод табеля на дату <paramref name="closeTo"/> (inclusive):
    /// - Закриває РІВНО один активний таймлайн (ClosedAt, ClosedBy, ClosedAtUtc).
    /// - Обрізає записи, що тягнуться за closeTo (To == null або To &gt; closeTo) до closeTo.
    /// - Soft-delete майбутніх записів (From &gt; closeTo) з заповненням Deleted* і причини.
    /// </summary>
    Task CloseOnExcludeAsync(
        Guid personId,
        DateOnly closeTo,
        string? reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
