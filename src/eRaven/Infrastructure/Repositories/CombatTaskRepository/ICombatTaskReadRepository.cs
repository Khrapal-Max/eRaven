//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// ICombatTaskPlanReadRepository
//-----------------------------------------------------------------------------

using eRaven.Application.DTOs.CombatTask;
using eRaven.Domain.Enums;

namespace eRaven.Infrastructure.Repositories.CombatTaskRepository;

/// <summary>
/// Read-репозиторій для сторінок планування бойових завдань (запити/вивід).
///
/// Ключова ідея:
/// - ТІЛЬКИ читання (AsNoTracking), БЕЗ мутацій.
/// - Повертає готові DTO для UI: місячний план, "хто на день", реєстр документів.
/// - Не містить бізнес-логіки проведення/відміни (це write-side).
///
/// Джерела даних (важливо для консистентності):
/// - "План на місяць" та "хто на день" — бажано будувати від CombatTaskAssignment
///   (денормалізований стан, швидко, мінімум JOIN).
/// - "Реєстр документів" — будувати від CombatTaskPlanDocument (+ агреговані Line-дані),
///   бо це аудит вводу і статуса документів.
///
/// Правила відображення:
/// - Canceled документи НЕ показуємо у плані.
/// - У планових сторінках зазвичай показуємо Draft + Posted (бо Draft важливий для плану),
///   а "факт" формується при Posted (коли з’являється/оновлюється Assignment).
/// - Реєстр документів має вміти фільтрувати за статусом (Draft/Posted/Canceled).
///
/// Пошук:
/// - search — це фільтр по ПІБ / РНОКПП, а також (за потреби) по назві документа.
/// - якщо search пустий або < 2 символів — можна ігнорувати (щоб не вбивати запит).
/// </summary>
public interface ICombatTaskReadRepository
{
    /// <summary>
    /// Повертає планування за місяць (таблична матриця/список).
    ///
    /// Призначення UI:
    /// - сторінка "План на місяць" (показати, хто і на які дні запланований/активний).
    ///
    /// Рекомендоване джерело:
    /// - CombatTaskAssignment (бо це вже "поточний стан" + денормалізовані атрибути).
    ///
    /// Семантика:
    /// - year/month визначають місяць.
    /// - search: ПІБ/РНОКПП (опційно).
    ///
    /// Очікування:
    /// - повертаються рядки з даними по особі та активності/плану в межах місяця.
    /// - сортування: EnrollmentKind/PositionSort/FullName (або логіка як у табелі).
    /// </summary>
    Task<IReadOnlyList<PlanningMonthAssignmentRowDto>> GetPlanningMonthAsync(
        int year,
        int month,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає реєстр документів планування за місяць.
    ///
    /// Призначення UI:
    /// - сторінка "Документи плану" (щоб знайти документ, відмінити Draft, перевірити Posted).
    ///
    /// Джерело:
    /// - CombatTaskPlanDocument (+ агрегування Lines за потреби).
    ///
    /// Семантика:
    /// - status: якщо null — повертаємо всі (Draft+Posted+Canceled).
    /// - search: може фільтрувати по PlanningDocTitle + (опційно) по ПІБ/РНОКПП з Lines.
    ///
    /// Очікування:
    /// - сортування: RecordedAt desc, потім PlanningDocTitle (або Id).
    /// - DTO має містити мінімум для реєстру: дати, назва документа, статус, кількість рядків, хто створив.
    /// </summary>
    Task<IReadOnlyList<PlanningDocumentRowDto>> GetPlanningDocumentsAsync(
        int year,
        int month,
        CombatTaskPlanDocumentStatus? status,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає планування за день ("хто на день" з групуванням).
    ///
    /// Призначення UI:
    /// - сторінка "Хто на день по плану" з групуванням:
    ///   PositionalArea / GroupName / AssetType / Mode / Goal + список людей.
    ///
    /// Рекомендоване джерело:
    /// - CombatTaskAssignment (щоб показувати актуальний стан задачі),
    ///   але з прив’язкою до PlanningDate/StartedAt/EndedAt (залежить від твоєї логіки).
    ///
    /// Семантика:
    /// - date — дата, для якої будуємо групи.
    /// - search — опційний фільтр по ПІБ/РНОКПП (всередині груп).
    ///
    /// Очікування:
    /// - Canceled документи не враховувати.
    /// - Draft може відображатися (як план), Posted — як факт/підтверджено.
    /// - сортування груп: PositionalArea, GroupName, AssetType, Mode, Goal.
    /// - сортування осіб в групі: Rank/Position/FullName (або як у табелі).
    /// </summary>
    Task<IReadOnlyList<PlanningDayGroupDto>> GetPlanningDayAsync(
        DateOnly date,
        string? search,
        CancellationToken ct = default);

    /// <summary>
    /// Повертає деталі документа планування за його ідентифікатором (header + lines) для UI-редактора.
    /// </summary>
    /// <remarks>
    /// Використання:
    /// - Сторінка <c>/planning-documents/{id}</c> (Document editor): показ шапки, список рядків, доступність дій Draft.
    /// - Сторінки <c>/planning-documents/{id}/line/new</c> та <c>/planning-documents/{id}/line/{lineId}</c>:
    ///   підвантаження контексту документа + (для edit) вибір існуючого рядка.
    ///
    /// Поведінка/контракти:
    /// - Read-only запит: <c>AsNoTracking</c>, без будь-яких мутацій.
    /// - Якщо документ не знайдено — повертає <c>null</c> (UI вирішує як показати помилку/redirect).
    /// - Рядки повертаються відсортованими (рекомендовано): <c>ActionDate</c> ↑, <c>Kind</c> ↑, <c>FullName</c> ↑, <c>Id</c> ↑
    ///   щоб відображення було стабільним при оновленнях/посторінкових перерендерингах.
    /// - Повертає і Draft, і Posted, і Canceled — але UI може обмежувати кнопки дій за статусом.
    /// </remarks>
    /// <param name="documentId">ID документа планування.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// DTO з полями документа та переліком рядків або <c>null</c>, якщо документ не існує.
    /// </returns>
    Task<CombatTaskPlanDocumentDetailsDto?> GetPlanningDocumentDetailsAsync(
        Guid documentId,
        CancellationToken ct = default);
}
