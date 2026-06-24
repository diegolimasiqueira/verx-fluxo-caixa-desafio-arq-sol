namespace CashFlow.DailyBalanceService.Api.DTOs;

/// <summary>Saldo diário consolidado de uma data.</summary>
public record DailyBalanceResponse(
    DateOnly Date,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal ConsolidatedBalance,
    DateTime UpdatedAt
);
