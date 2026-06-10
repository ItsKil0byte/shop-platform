using Cart.Application.Interfaces;
using Cart.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cart.Infrastructure.Persistence;

public class CartRepository : ICartRepository
{
    private readonly CartDbContext _context;

    public CartRepository(CartDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CartItem>> GetByUserIdAsync(string userId)
    {
        return await _context.CartItems.Where(i => i.UserId == userId).ToListAsync();
    }

    public async Task AddItemAsync(CartItem item)
    {
        await _context.CartItems.AddAsync(item);
    }

    public async Task ClearCartAsync(string userId)
    {
        var items = await _context.CartItems.Where(i => i.UserId == userId).ToListAsync();
        _context.CartItems.RemoveRange(items);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}