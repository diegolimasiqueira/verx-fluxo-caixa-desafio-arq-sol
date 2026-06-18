namespace CashFlow.LaunchService.Api.DTOs;

/// <summary>Representação de um lançamento financeiro.</summary>
public record LaunchResponse(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string Type,
    string Description,
    DateTime CreatedAt
);
