using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IJwtTokenService
{
    string IssueToken(UserEntity user);
}

