using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class PaymentEntity
{
    public Guid Id { get; private set; }
    public string OrderId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; }
    public string? TransactionId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PaymentEntity() {} // Для EF Core

    public PaymentEntity(string orderId, string userId, decimal amount)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsSuccess(string transactionId)
    {
        Status = PaymentStatus.Success;
        TransactionId = transactionId;
    }

    public void MarkAsFailed()
    {
        Status = PaymentStatus.Failed;
    }
}
