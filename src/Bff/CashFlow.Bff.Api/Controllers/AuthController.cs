using CashFlow.Bff.Api.DTOs;
using CashFlow.Bff.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CashFlow.Bff.Api.Controllers;

/// <summary>Autenticação — obtenha o JWT para usar nos demais endpoints.</summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    /// <summary>Autentica o usuário e retorna um token JWT.</summary>
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var token = await authService.AuthenticateAsync(request.Email, request.Password, ct);

        if (token is null)
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(new LoginResponse(token, "Bearer", 86400));
    }
}
