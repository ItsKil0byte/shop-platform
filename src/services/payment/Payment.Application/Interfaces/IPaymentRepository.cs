using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentRepository
{
    Task AddAsync(PaymentEntity payment, CancellationToken cancellationToken = default);
    Task<PaymentEntity?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
}
