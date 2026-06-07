using Microsoft.AspNetCore.Mvc;
using Order.Application.DTOs;
using Order.Application.Interfaces;
using Order.Domain.Entities;

namespace Order.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrderController(IOrderService orderService, IOrderRepository orderRepository) : ControllerBase
{
    private readonly IOrderService _orderService = orderService;
    private readonly IOrderRepository _orderRepository = orderRepository;

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            return BadRequest("ID пользователя не может быть пустым.");
        }

        Guid orderId = await _orderService.CreateOrderAsync(request.UserId);

        OrderEntity? order = await _orderRepository.GetByIdAsync(orderId);

        if (order == null)
        {
            return NotFound("Заказ не найден.");
        }

        OrderResponse response = new(order.Id.ToString(), order.Status.ToString());
        
        return Ok(response);
    }
}