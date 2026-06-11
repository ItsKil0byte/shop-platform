using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IUserRepository
{
    Task<UserEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserEntity?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task AddAsync(UserEntity user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
