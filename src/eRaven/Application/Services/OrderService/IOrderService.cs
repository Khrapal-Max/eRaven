//-----------------------------------------------------------------------------
// All rights by agreement of the developer. Author data on GitHub Khrapal M.G.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------
// IOrderService
//-----------------------------------------------------------------------------

using eRaven.Application.ViewModels.OrderViewModels;
using eRaven.Domain.Models;

namespace eRaven.Application.Services.OrderService;

public interface IOrderService
{
    Task<List<Order>> GetAllAsync(CancellationToken ct = default);

    Task<Order> CreateOrderAsync(CreateOrderViewModel model, CancellationToken ct = default);

    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);

    /// <summary>
    /// Підтверджує (approve) наказ і застосовує зміни статусів
    /// </summary>
    Task<bool> ApproveOrderAsync(Guid orderId, CancellationToken ct = default);
}
