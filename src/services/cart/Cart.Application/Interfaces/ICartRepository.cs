using Cart.Domain.Entities;

namespace Cart.Application.Interfaces;

public interface ICartRepository
{
    Task<IEnumerable<CartItem>> GetByUserIdAsync(string userId);
    Task AddItemAsync(CartItem item);
    Task ClearCartAsync(string userId);
    Task SaveChangesAsync();
}