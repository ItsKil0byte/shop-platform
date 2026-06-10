using Auth.Application.DTOs;
using Auth.Application.Results;

namespace Auth.Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
