//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
// IOrderService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.OrderViewModels;

namespace eRaven.Application.Services.OrderService;

public interface IOrderService
{
    /// <summary>
    /// Повертає всі накази
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>IEnumerable OrderViewModel(<see cref="OrderViewModel"/>)</returns>
    Task<IEnumerable<OrderViewModel>> GetAllOrderAsync(CancellationToken ct = default);

    /// <summary>
    /// Знайти наказ по Id (із планами та діями). Повертає null, якщо не знайдено.
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="ct"></param>
    /// <returns>OrderDetailsViewModel?(<see cref="OrderDetailsViewModel"/>)</returns>
    Task<OrderDetailsViewModel?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Створити (опублікувати) добовий наказ: закрити кілька планів і підтвердити вибрані дії.
    /// Повертає сам наказ + перелік закритих планів і підтверджених дій.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns>ExecutedPublishDailyOrderViewModel(<see cref="ExecutedPublishDailyOrderViewModel"/>)</returns>
    Task<ExecutedPublishDailyOrderViewModel> CreateAsync(CreatePublishDailyOrderViewModel createPublishDailyOrder, CancellationToken ct = default);

    /// <summary>
    /// Видалити наказ по Id. Повертає true, якщо видалено.
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="ct"></param>
    /// <returns>bool</returns>
    Task<bool> DeleteAsync(Guid orderId, CancellationToken ct = default);
}
