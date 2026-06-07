namespace Order.Application.Interfaces;

public interface IPaymentClient
{
    Task<(bool Success, string? TransactionId)> ProcessPaymentAsync(string orderId, string userId, decimal amount, CancellationToken cancellationToken = default);
}