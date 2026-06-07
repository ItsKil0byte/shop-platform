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
    IModerationClient moderationClient
) : IOrderService
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IEventPublisher _eventPublisher = eventPublisher;

    private readonly ICartClient _cartClient = cartClient;
    private readonly IPaymentClient _paymentClient = paymentClient;
    private readonly IModerationClient _moderationClient = moderationClient;

    public async Task<Guid> CreateOrderAsync(string userId, CancellationToken cancellationToken = default)
    {
        // 1. Создаем новый заказ со статусом "Создан"

        OrderEntity order = new(userId);
        await _orderRepository.AddAsync(order, cancellationToken);

        try
        {
            // 2. Модерация пользователя

            order.UpdateStatus(OrderStatus.CheckingUser);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            bool isBlocked = await _moderationClient.IsUserBannedAsync(userId);
            if (isBlocked)
            {
                await RollbackOrderAsync(order, "Пользователь заблокирован", cancellationToken);
                return order.Id;
            }

            // 3. Получаем информацию о корзине

            (decimal totalPrice, List<CartItemDto> cartItems) = await _cartClient.GetCartAsync(userId);
            if (totalPrice <= 0 || cartItems.Count == 0)
            {
                await RollbackOrderAsync(order, "Корзина пуста", cancellationToken);
                return order.Id;
            }

            order.UpdateTotalPrice(totalPrice);

            // 4. Пытаемся оплатить заказ

            order.UpdateStatus(OrderStatus.PaymentProcessing);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            // 5. Процесс оплаты

            (bool isSuccess, string? paymentId) = await _paymentClient.ProcessPaymentAsync(order.Id.ToString(), userId, totalPrice);

            if (!isSuccess)
            {
                await RollbackOrderAsync(order, "Оплата не удалась", cancellationToken);
                return order.Id;
            }

            // 6. Очищаем корзину

            await _cartClient.ClearCartAsync(userId, cancellationToken);

            // 7. Вы великолепны!

            order.UpdateStatus(OrderStatus.Paid);
            await _orderRepository.UpdateAsync(order, cancellationToken);

            await _eventPublisher.PublishOrderPaidEventAsync(order.Id, userId, cancellationToken);
        }
        catch (Exception ex)
        {
            await RollbackOrderAsync(order, $"Исключение: {ex.Message}", cancellationToken);
        }

        return order.Id;
        
    }

    public async Task RollbackOrderAsync(OrderEntity order, string reason, CancellationToken cancellationToken = default)
    {
        order.UpdateStatus(OrderStatus.Cancelled);
        await _orderRepository.UpdateAsync(order, cancellationToken);
        await _eventPublisher.PublishOrderCancelledEventAsync(order.Id, cancellationToken);
    }
}
