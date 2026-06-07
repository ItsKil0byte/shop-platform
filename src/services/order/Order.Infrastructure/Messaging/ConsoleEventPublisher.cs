using Order.Application.Interfaces;

namespace Order.Infrastructure.Messaging;

public class ConsoleEventPublisher : IEventPublisher
{
    public Task PublishOrderCancelledEventAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Заказ {orderId} отменен.");

        return Task.CompletedTask;
    }

    public Task PublishOrderPaidEventAsync(Guid orderId, string userId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Заказ {orderId} для пользователя {userId} оплачен.");

        return Task.CompletedTask;
    }
}