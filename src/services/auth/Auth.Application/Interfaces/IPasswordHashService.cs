using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(UserEntity user, string password);
    bool VerifyPassword(UserEntity user, string passwordHash, string password);
}

