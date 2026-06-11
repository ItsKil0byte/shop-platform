using Grpc.Core;
using Payment.Grpc;
using Order.Application.Interfaces;

namespace Order.Infrastructure.GrpcClients;

public class PaymentGrpcClient(PaymentService.PaymentServiceClient client) : IPaymentClient
{
    private readonly PaymentService.PaymentServiceClient _client = client;

    public async Task<(bool Success, string? TransactionId)> ProcessPaymentAsync(string orderId, string userId, decimal amount, CancellationToken cancellationToken = default)
    {
        PaymentRequest request = new()
        {
            OrderId = orderId,
            UserId = userId,
            Amount = (double)amount
        };

        DateTime deadline = DateTime.UtcNow.AddSeconds(5);

        try
        {
            PaymentResponse response = await _client.ProcessPaymentAsync(
                request,
                deadline: deadline,
                cancellationToken: cancellationToken);

            return (response.Success, response.TransactionId);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.DeadlineExceeded)
        {
            throw new TimeoutException("Запрос к сервису оплаты превысил время ожидания.", ex);
        }
    }
}
