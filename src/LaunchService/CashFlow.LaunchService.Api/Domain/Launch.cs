using CashFlow.LaunchService.Api.Middleware;

namespace CashFlow.LaunchService.Api.Domain;

public class Launch
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Amount { get; private set; }
    public LaunchType Type { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private Launch() { }

    public static Launch Create(DateOnly date, decimal amount, LaunchType type, string description)
    {
        if (amount <= 0)
            throw new DomainException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Description is required.");

        if (description.Length > 255)
            throw new DomainException("Description must not exceed 255 characters.");

        return new Launch
        {
            Id = Guid.NewGuid(),
            Date = date,
            Amount = amount,
            Type = type,
            Description = description.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}

public enum LaunchType
{
    Credit,
    Debit
}
