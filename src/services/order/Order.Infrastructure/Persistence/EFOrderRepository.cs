using Microsoft.EntityFrameworkCore;
using Order.Application.Interfaces;
using Order.Domain.Entities;

namespace Order.Infrastructure.Persistence;

public class EFOrderRepository(OrderDBContext context) : IOrderRepository
{
    private readonly OrderDBContext _context = context;

    public async Task AddAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders.FindAsync([id], cancellationToken);
    }

    public async Task UpdateAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync(cancellationToken);
    }
}