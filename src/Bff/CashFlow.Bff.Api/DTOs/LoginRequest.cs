using System.ComponentModel.DataAnnotations;

namespace CashFlow.Bff.Api.DTOs;

/// <summary>Credenciais para autenticação.</summary>
public record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password
);

/// <summary>Token JWT retornado após autenticação bem-sucedida.</summary>
public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn
);
