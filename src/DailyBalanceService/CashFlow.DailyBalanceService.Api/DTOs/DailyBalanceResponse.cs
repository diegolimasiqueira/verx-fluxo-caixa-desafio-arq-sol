namespace CashFlow.DailyBalanceService.Api.DTOs;

/// <summary>Saldo diário consolidado de uma data.</summary>
public record DailyBalanceResponse(
    DateOnly Date,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal ConsolidatedBalance,
    DateTime UpdatedAt
);

/// <summary>Credenciais para autenticação.</summary>
public record LoginRequest(string Username, string Password);

/// <summary>Token JWT retornado após autenticação bem-sucedida.</summary>
public record LoginResponse(string AccessToken, string TokenType, int ExpiresIn);
