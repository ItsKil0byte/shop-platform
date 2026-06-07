using System.Collections.Concurrent;
using Order.Application.Interfaces;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence;

public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, OrderEntity> _orders = new();

    public Task AddAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;

        Console.WriteLine($"Добавлен заказ {order.Id} для пользователя {order.UserId}");

        return Task.CompletedTask;
    }

    public Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _orders.TryGetValue(id, out OrderEntity? order);
        return Task.FromResult(order);
    }

    public Task UpdateAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        _orders[order.Id] = order;

        Console.WriteLine($"Обновлен заказ {order.Id} для пользователя {order.UserId}");

        return Task.CompletedTask;
    }
}