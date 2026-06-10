namespace Auth.Application.DTOs;

public sealed record RegisterRequest(string Email, string Password, string? Name);

