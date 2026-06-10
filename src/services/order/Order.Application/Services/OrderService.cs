using Microsoft.Extensions.Logging;
using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;
using Order.Domain.Enums;

namespace Order.Application.Services;

public class OrderService(
    IOrderRepository orderRepository,
    IEventPublisher eventPublisher,
    ICartClient cartClient,
    IPaymentClient paymentClient,
    IModerationClient moderationClient,
    ILogger<OrderService> logger
) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IEventPublisher _eventPublisher = eventPublisher;

    private readonly ICartClient _cartClient = cartClient;
    private readonly IPaymentClient _paymentClient = paymentClient;
    private readonly IModerationClient _moderationClient = moderationClient;
    private readonly ILogger<OrderService> _logger = logger;

    public async Task<Guid> CreateOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        // 1. Создаем новый заказ со статусом "Создан"

        _logger.LogInformation("Создаём заказ для пользователя {UserId}", userId);

        OrderEntity order = new(userId);
        await _orderRepository.AddAsync(order, cancellationToken);

        try
        {
            // 2. Модерация пользователя

            _logger.LogInformation("[1/4]: Проверяем не забанен ли пользователь. Заказ: {OrderId}, Пользователь: {UserId}", order.Id, userId);

            order.UpdateStatus(OrderStatus.CheckingUser);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            bool isBlocked = await _moderationClient.IsUserBannedAsync(userId, cancellationToken);
            if (isBlocked)
            {
                _logger.LogWarning("[1/4] (ОТКАТ): Пользователь {UserId} заблокирован. Откатываем заказ {OrderId}", userId, order.Id);
                await RollbackOrderAsync(order, "Пользователь заблокирован", cancellationToken);
                return order.Id;
            }

            // 3. Получаем информацию о корзине

            _logger.LogInformation("[2/4]: Получаем корзину для пользователя {UserId}", userId);

            (decimal totalPrice, List<CartItemDto> cartItems) = await _cartClient.GetCartAsync(userId, cancellationToken);
            if (totalPrice <= 0 || cartItems.Count == 0)
            {
                _logger.LogWarning("[2/4] (ОТКАТ): Корзина пользователя {UserId} пуста. Откатываем заказ {OrderId}", userId, order.Id);
                await RollbackOrderAsync(order, "Корзина пуста", cancellationToken);
                return order.Id;
            }

            order.UpdateTotalPrice(totalPrice);

            // 4. Пытаемся оплатить заказ

            _logger.LogInformation("[3/4]: Оплата {Amount} для заказа {OrderId} пользователя {UserId}", totalPrice, order.Id, userId);

            order.UpdateStatus(OrderStatus.PaymentProcessing);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            // 5. Процесс оплаты

            (bool isSuccess, string? paymentId) = await _paymentClient.ProcessPaymentAsync(order.Id.ToString(), userId, totalPrice, cancellationToken);

            if (!isSuccess)
            {
                _logger.LogWarning("[3/4] (ОТКАТ): Не удалось оплатить заказ {OrderId}.", order.Id);
                await RollbackOrderAsync(order, "Оплата не удалась", cancellationToken);
                return order.Id;
            }

            // 6. Очищаем корзину

            _logger.LogInformation("[4/4]: Очищаем корзину пользователя {UserId} после успешной оплаты: {PaymentId}", userId, paymentId);
            await _cartClient.ClearCartAsync(userId, cancellationToken);

            // 7. Вы великолепны!

            order.UpdateStatus(OrderStatus.Paid);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            _logger.LogInformation("Транзакиця завершилась успешно! Заказ {OrderId} оплачен. Транзакция: {PaymentId}", order.Id, paymentId);

            await _eventPublisher.PublishOrderPaidEventAsync(order.Id, userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Возникла проблема при работе транзакции. Выполняем откат заказа {OrderId}", order.Id);
            await RollbackOrderAsync(order, $"Исключение: {ex.Message}", cancellationToken);
        }

        return order.Id;

    }

    private async Task RollbackOrderAsync(OrderEntity order, string reason, CancellationToken cancellationToken = default)
    {
        order.UpdateStatus(OrderStatus.Cancelled);
        await _orderRepository.UpdateAsync(order, cancellationToken);

        _logger.LogWarning("Откат транзакции. Заказ {OrderId} отменён. Причина: {Reason}", order.Id, reason);

        await _eventPublisher.PublishOrderCancelledEventAsync(order.Id, cancellationToken);
    }
}
