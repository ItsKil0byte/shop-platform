using System.Net.Mail;
using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Application.Results;
using Auth.Domain.Entities;

namespace Auth.Application.Services;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHashService passwordHashService,
    IJwtTokenService jwtTokenService) : IAuthService
{
    private const int MinimumPasswordLength = 8;

    public async Task<ServiceResult<AuthResponse>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        string? validationError = ValidateEmailAndPassword(request.Email, request.Password);
        if (validationError is not null)
        {
            return ServiceResult<AuthResponse>.Failure(AuthError.Validation, validationError);
        }

        string normalizedEmail = UserEntity.NormalizeEmail(request.Email);
        UserEntity? existing = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return ServiceResult<AuthResponse>.Failure(AuthError.DuplicateEmail, "Email is already registered.");
        }

        UserEntity user = new(request.Email, request.Name);
        user.SetPasswordHash(passwordHashService.HashPassword(user, request.Password));

        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return ServiceResult<AuthResponse>.Success(CreateAuthResponse(user));
    }

    public async Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ServiceResult<AuthResponse>.Failure(AuthError.Validation, "Email and password are required.");
        }

        string normalizedEmail = UserEntity.NormalizeEmail(request.Email);
        UserEntity? user = await userRepository.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);
        if (user?.PasswordHash is null)
        {
            return ServiceResult<AuthResponse>.Failure(AuthError.InvalidCredentials, "Invalid credentials.");
        }

        bool passwordIsValid = passwordHashService.VerifyPassword(user, user.PasswordHash, request.Password);
        if (!passwordIsValid)
        {
            return ServiceResult<AuthResponse>.Failure(AuthError.InvalidCredentials, "Invalid credentials.");
        }

        return ServiceResult<AuthResponse>.Success(CreateAuthResponse(user));
    }

    public async Task<ServiceResult<UserDto>> GetCurrentUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        UserEntity? user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<UserDto>.Failure(AuthError.NotFound, "User not found.");
        }

        return ServiceResult<UserDto>.Success(ToDto(user));
    }

    private AuthResponse CreateAuthResponse(UserEntity user)
    {
        return new AuthResponse(jwtTokenService.IssueToken(user), ToDto(user));
    }

    private static UserDto ToDto(UserEntity user)
    {
        return new UserDto(user.Id, user.Email, user.Name);
    }

    private static string? ValidateEmailAndPassword(string email, string password)
    {
        if (!IsValidEmail(email))
        {
            return "Valid email is required.";
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumPasswordLength)
        {
            return $"Password must be at least {MinimumPasswordLength} characters.";
        }

        return null;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            _ = new MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
