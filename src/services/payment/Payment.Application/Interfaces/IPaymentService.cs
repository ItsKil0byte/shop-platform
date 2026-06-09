namespace Payment.Application.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Обрабатывает платёж. Возвращает (success, transactionId).
    /// Идемпотентен: повторный вызов с тем же orderId вернёт уже сохранённый результат.
    /// </summary>
    Task<(bool Success, string? TransactionId)> ProcessPaymentAsync(
        string orderId,
        string userId,
        decimal amount,
        CancellationToken cancellationToken = default);
}
