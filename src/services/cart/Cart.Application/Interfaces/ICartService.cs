using Cart.Application.DTOs;

namespace Cart.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(string userId);

    Task AddItemToCartAsync(AddToCartDto dto);

    Task ClearCartAsync(string userId);
}
