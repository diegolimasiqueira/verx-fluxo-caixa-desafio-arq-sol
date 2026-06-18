using System.ComponentModel.DataAnnotations;

namespace CashFlow.LaunchService.Api.DTOs;

/// <summary>
/// Payload para registro de um novo lançamento financeiro.
/// </summary>
public record RegisterLaunchRequest(
    /// <summary>Data do lançamento (formato: yyyy-MM-dd)</summary>
    [Required] DateOnly Date,

    /// <summary>Valor do lançamento. Deve ser maior que zero.</summary>
    [Required][Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    decimal Amount,

    /// <summary>Tipo do lançamento: 'credit' para crédito ou 'debit' para débito.</summary>
    [Required][RegularExpression("credit|debit", ErrorMessage = "Type must be 'credit' or 'debit'.")]
    string Type,

    /// <summary>Descrição do lançamento. Máximo de 255 caracteres.</summary>
    [Required][MaxLength(255)]
    string Description
);
