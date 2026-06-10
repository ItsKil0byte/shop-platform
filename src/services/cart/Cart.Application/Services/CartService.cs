using Cart.Application.DTOs;
using Cart.Application.Interfaces;
using Cart.Domain.Entities;

namespace Cart.Application.Services;

public class CartService : ICartService
{
    private readonly ICartRepository _repository;

    public CartService(ICartRepository repository)
    {
        _repository = repository;
    }

    public async Task<CartDto> GetCartAsync(string userId)
    {
        var items = await _repository.GetByUserIdAsync(userId);
        
        var cartItems = items.Select(i => new CartItemDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Price = i.Price
        }).ToList();

        return new CartDto
        {
            UserId = userId,
            Items = cartItems,
            TotalPrice = cartItems.Sum(i => i.Price * i.Quantity)
        };
    }

    public async Task AddItemToCartAsync(AddToCartDto dto)
    {
        var item = new CartItem
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            ProductId = dto.ProductId,
            Quantity = dto.Quantity,
            Price = dto.Price
        };

        await _repository.AddItemAsync(item);
        await _repository.SaveChangesAsync();
    }

    public async Task ClearCartAsync(string userId)
    {
        await _repository.ClearCartAsync(userId);
        await _repository.SaveChangesAsync();
    }
}