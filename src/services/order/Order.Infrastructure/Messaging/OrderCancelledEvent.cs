namespace Order.Infrastructure.Messaging.Events;

public record OrderCancelledEvent(string OrderId);
