using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Auth.Application.DTOs;
using Auth.Application.Interfaces;
using Auth.Application.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<AuthResponse> result = await authService.RegisterAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        ServiceResult<AuthResponse> result = await authService.LoginAsync(request, cancellationToken);
        return ToActionResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        string? subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(subject, out Guid userId))
        {
            return Unauthorized(new { message = "Invalid token." });
        }

        ServiceResult<UserDto> result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return ToActionResult(result);
    }

    private ActionResult<T> ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(result.Value);
        }

        object error = new { message = result.Message };
        return result.Error switch
        {
            AuthError.Validation => BadRequest(error),
            AuthError.DuplicateEmail => Conflict(error),
            AuthError.InvalidCredentials => Unauthorized(error),
            AuthError.NotFound => Unauthorized(error),
            _ => StatusCode(StatusCodes.Status500InternalServerError, error)
        };
    }
}
