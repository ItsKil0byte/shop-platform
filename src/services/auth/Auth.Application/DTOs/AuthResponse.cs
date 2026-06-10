namespace Auth.Application.DTOs;

public sealed record AuthResponse(string Token, UserDto User);

