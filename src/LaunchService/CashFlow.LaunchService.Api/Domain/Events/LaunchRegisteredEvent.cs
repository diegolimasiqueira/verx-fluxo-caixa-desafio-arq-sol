namespace CashFlow.LaunchService.Api.Domain.Events;

public record LaunchRegisteredEvent
{
    public Guid LaunchId { get; init; }
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
