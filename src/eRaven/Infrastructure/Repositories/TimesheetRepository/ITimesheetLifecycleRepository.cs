//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ITimesheetLifecycleRepository
//-----------------------------------------------------------------------------

namespace eRaven.Infrastructure.Repositories.TimesheetRepository;

/// <summary>
/// Репозиторій життєвого циклу табеля (відкриття/закриття шкал та базові інваріанти).
///
/// Призначення:
/// - При зарахуванні (Enroll): гарантувати наявність активних шкал (timelines) та дефолтного Main=30.
/// - При виключенні (Exclude): перевірити, що закриття дозволено, і виконати закриття з нормалізацією записів.
///
/// Важливі правила домену:
/// - Табель є фактом (source of truth) для "стану" особи по датах.
/// - NB ("НБ") не зберігаємо як entry — це derived-стан, коли немає активного Main entry на дату.
/// - Діапазони інклюзивні: [From..To]. Якщо To == null — запис відкритий у майбутнє.
/// - Закриття timeline на дату D означає: наступний день (D+1) вже поза табелем.
/// </summary>
public interface ITimesheetLifecycleRepository
{
    /// <summary>
    /// Відкриває табель при зарахуванні особи:
    /// - створює активні шкали (timelines) для особи (Main/Task), якщо їх ще немає;
    /// - додає дефолтний запис Main з кодом "30" (open-ended), який покриває <paramref name="enrollDate"/>,
    ///   якщо такого запису ще немає.
    ///
    /// Очікувана поведінка: операція ідемпотентна (повторний виклик не створює дублікати).
    /// </summary>
    Task OpenOnEnrollAsync(
        Guid personId,
        DateOnly enrollDate,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);

    /// <summary>
    /// Перевіряє (без внесення змін), чи дозволено закривати табель при виключенні на дату <paramref name="closeTo"/>.
    ///
    /// Правило:
    /// - На дату <paramref name="closeTo"/> активний Main-запис має бути у дозволеному стані
    ///   (наприклад, "30" або "РОЗПОР" — визначається реалізацією).
    ///
    /// Якщо умови не виконані — кидає <see cref="InvalidOperationException"/> з поясненням.
    /// </summary>
    Task ValidateCanCloseOnExcludeAsync(
        Guid personId,
        DateOnly closeTo,
        CancellationToken ct = default);

    /// <summary>
    /// Закриває табель при виключенні особи:
    /// - закриває всі активні шкали (ClosedAt = <paramref name="closeTo"/>, audit-поля ClosedBy/ClosedAtUtc);
    /// - обрізає записи, що тягнуться за <paramref name="closeTo"/> (To == null або To &gt; closeTo) до closeTo;
    /// - soft-delete майбутніх записів (From &gt; closeTo) з заповненням Deleted* і причини.
    ///
    /// Реалізація повинна виконувати операцію в транзакції і перед внесенням змін
    /// перевірити дозволеність стану (еквівалентно <see cref="ValidateCanCloseOnExcludeAsync"/>).
    /// </summary>
    Task CloseOnExcludeAsync(
        Guid personId,
        DateOnly closeTo,
        string reason,
        string author,
        DateTime nowUtc,
        CancellationToken ct = default);
}
