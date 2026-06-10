using Grpc.Core;
using Payment.Application.Interfaces;
using Payment.Grpc;

namespace Payment.Infrastructure.GrpcServer;

/// <summary>
/// gRPC-сервер: реализует контракт payment.proto
/// </summary>
public class PaymentGrpcService(IPaymentService paymentService) : PaymentService.PaymentServiceBase
{
    private readonly IPaymentService _paymentService = paymentService;

    public override async Task<PaymentResponse> ProcessPayment(
        PaymentRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId) ||
            string.IsNullOrWhiteSpace(request.UserId) ||
            request.Amount <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Неверные параметры платежа."));
        }

        (bool success, string? transactionId) = await _paymentService.ProcessPaymentAsync(
            request.OrderId,
            request.UserId,
            (decimal)request.Amount,
            context.CancellationToken);

        return new PaymentResponse
        {
            Success = success,
            TransactionId = transactionId ?? string.Empty
        };
    }
}
