namespace Order.Infrastructure.Messaging.Events;

public record OrderPaidEvent(string OrderId, string UserId);
