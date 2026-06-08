namespace Order.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishOrderPaidEventAsync(Guid orderId, string userId, CancellationToken cancellationToken = default);
    Task PublishOrderCancelledEventAsync(Guid orderId, CancellationToken cancellationToken = default);
}