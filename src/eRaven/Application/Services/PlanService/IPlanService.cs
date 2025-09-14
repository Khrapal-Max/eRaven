// -----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
// -----------------------------------------------------------------------------
// IPlanService (мінімальний контракт)
// -----------------------------------------------------------------------------

using eRaven.Application.ViewModels.PlanViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.PlanService;

public interface IPlanService
{
    /// <summary>
    /// Повернути всі плани (без вкладених елементів).
    /// </summary>
    /// <remarks>
    /// Використовується на сторінці списку. Очікується швидке завантаження:
    /// <c>AsNoTracking</c>, сортування за <c>RecordedUtc DESC</c>.
    /// </remarks>
    Task<IEnumerable<Plan>> GetAllPlansAsync(CancellationToken ct = default);

    /// <summary>
    /// Повернути план за ідентифікатором (із вкладеними елементами та снапшотами).
    /// </summary>
    /// <remarks>
    /// Використовується сторінкою деталей. Має eagerly include:
    /// <list type="bullet">
    /// <item><description><c>Plan.PlanElements</c> (ordered by <c>EventAtUtc</c>)</description></item>
    /// <item><description><c>PlanElement.PlanParticipantSnapshot</c></description></item>
    /// </list>
    /// Повертає <see langword="null"/>, якщо план не знайдено.
    /// </remarks>
    Task<Plan?> GetByIdAsync(Guid planId, CancellationToken ct = default);

    /// <summary>
    /// Створити новий план.
    /// </summary>
    /// <param name="vm">Дані для створення (номер, стан).</param>
    /// <returns>Створений <see cref="Plan"/> без елементів.</returns>
    /// <exception cref="ArgumentNullException">Якщо <paramref name="vm"/> або номер плану порожні.</exception>
    /// <exception cref="InvalidOperationException">Якщо план з таким номером уже існує.</exception>
    Task<Plan> CreateAsync(CreatePlanViewModel vm, CancellationToken ct = default);

    /// <summary>
    /// Закрити план (перевести зі стану Open у Closed).
    /// </summary>
    /// <returns>
    /// <see langword="true"/> — якщо стан змінено;<br/>
    /// <see langword="false"/> — якщо план не знайдено.
    /// </returns>
    /// <exception cref="InvalidOperationException">Якщо план уже закрито.</exception>
    Task<bool> CloseAsync(Guid planId, CancellationToken ct = default);

    /// <summary>
    /// Видалити план, якщо він відкритий.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> — якщо видалено;<br/>
    /// <see langword="false"/> — якщо план не знайдено.
    /// </returns>
    /// <exception cref="InvalidOperationException">Якщо план закритий — видалення заборонено.</exception>
    Task<bool> DeleteIfOpenAsync(Guid planId, CancellationToken ct = default);

    // ---- Plan elements ----

    /// <summary>
    /// Додати кілька елементів (однакові атрибути події для множини осіб) і повернути створені елементи.
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="items"></param>
    /// <param name="ct"></param>
    /// <returns>IReadOnlyList PlanElement(<see cref="PlanElement"/>)</returns>
    Task<IReadOnlyList<PlanElement>> AddElementsAsync(Guid planId, IEnumerable<CreatePlanElementViewModel> items, CancellationToken ct = default);

    /// <summary>
    /// Видалити елемент із відкритого плану (PPS каскадом). false — якщо не знайдено.
    /// </summary>
    /// <param name="planId"></param>
    /// <param name="elementId"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> RemoveElementAsync(Guid planId, Guid elementId, CancellationToken ct = default);
}
