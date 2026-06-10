using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Services;

public class PaymentService(IPaymentRepository paymentRepository) : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository = paymentRepository;

    public async Task<(bool Success, string? TransactionId)> ProcessPaymentAsync(
        string orderId,
        string userId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        // Идемпотентность: если платёж по этому заказу уже есть — вернуть сохранённый результат
        PaymentEntity? existing = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);
        if (existing != null)
        {
            return (existing.Status == Domain.Enums.PaymentStatus.Success, existing.TransactionId);
        }

        PaymentEntity payment = new(orderId, userId, amount);

        // Имитация оплаты: 80% успех, 20% отказ
        // В реальном сервисе здесь был бы вызов платёжного провайдера
        bool isSuccess = Random.Shared.NextDouble() > 0.2;

        if (isSuccess)
        {
            string transactionId = Guid.NewGuid().ToString();
            payment.MarkAsSuccess(transactionId);
            await _paymentRepository.AddAsync(payment, cancellationToken);
            return (true, transactionId);
        }
        else
        {
            payment.MarkAsFailed();
            await _paymentRepository.AddAsync(payment, cancellationToken);
            return (false, null);
        }
    }
}
