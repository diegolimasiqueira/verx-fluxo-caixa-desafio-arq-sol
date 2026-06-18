using CashFlow.LaunchService.Api.DTOs;
using CashFlow.LaunchService.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.LaunchService.Api.Controllers;

/// <summary>Autenticação — obtenha o JWT para usar nos demais endpoints.</summary>
[ApiController]
[Route("api/auth")]
[Tags("Auth")]
public class AuthController(TokenService tokenService) : ControllerBase
{
    /// <summary>Autentica o usuário e retorna um token JWT.</summary>
    /// <remarks>
    /// Use as credenciais de teste: **username:** admin | **password:** admin
    /// 
    /// Copie o `accessToken` retornado e clique em **Authorize** no topo da página para usá-lo nos demais endpoints.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var token = tokenService.GenerateToken(request.Username, request.Password);

        if (token is null)
            return Unauthorized(new { message = "Invalid credentials." });

        return Ok(new LoginResponse(token, "Bearer", 86400));
    }
}
