using Catalog.Domain.Entities;

namespace Catalog.Application.Interfaces;

public interface ICatalogRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task AddAsync(Product product);
    Task SaveChangesAsync();
}