using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Auth.Infrastructure.Security;

public class PasswordHashService(IPasswordHasher<UserEntity> passwordHasher) : IPasswordHashService
{
    public string HashPassword(UserEntity user, string password)
    {
        return passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(UserEntity user, string passwordHash, string password)
    {
        PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(user, passwordHash, password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}

