namespace eRaven.Application.Services.OrderService;

public interface IOrderService
{
    Task<bool> AppointAsync(Guid personId, Guid positionUnitId, DateTime effectiveUtc,
                           string? note, string? author, CancellationToken ct = default);

    Task<bool> TransferAsync(Guid personId, Guid newPositionUnitId, DateTime effectiveUtc,
                             string? note, string? author, CancellationToken ct = default);

    Task<bool> DismissAsync(Guid personId, DateTime effectiveUtc,
                            string? note, string? author, CancellationToken ct = default);
}
