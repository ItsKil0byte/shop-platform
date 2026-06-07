using Order.Domain.Entities;

namespace Order.Application.Interfaces;

public interface IOrderService
{
    Task<Guid> CreateOrderAsync(string userId, CancellationToken cancellationToken = default);
    Task RollbackOrderAsync(OrderEntity order, string reason, CancellationToken cancellationToken = default);
}