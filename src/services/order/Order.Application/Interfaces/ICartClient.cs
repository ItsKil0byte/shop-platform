using Order.Application.DTOs;

namespace Order.Application.Interfaces;

public interface ICartClient
{
    Task<(decimal TotalPrice, List<CartItemDto> Items)> GetCartAsync(string userId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(string userId, CancellationToken cancellationToken = default);
}