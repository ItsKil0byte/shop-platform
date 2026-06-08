using Order.Domain.Entities;

namespace Order.Application.Interfaces;

public interface IOrderRepository
{
    Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task UpdateAsync(OrderEntity order, CancellationToken cancellationToken = default);
}