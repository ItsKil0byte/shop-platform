using Order.Application.Interfaces;

namespace Order.Infrastructure.GrpcClients;

public class PaymentGrpcClient : IPaymentClient
{
    public Task<(bool Success, string? TransactionId)> ProcessPaymentAsync(string orderId, string userId, decimal amount, CancellationToken cancellationToken = default)
    {
        // Простая заглушка: всегда успешная оплата с новым TransactionId.
        return Task.FromResult<(bool, string?)>((true, Guid.NewGuid().ToString()));
    }
}
