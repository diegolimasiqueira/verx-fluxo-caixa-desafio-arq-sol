using CashFlow.DailyBalanceService.Api.Domain;
using FluentAssertions;

namespace CashFlow.DailyBalanceService.Tests.Domain;

public class DailyBalanceTests
{
    [Fact]
    public void ConsolidatedBalance_ShouldBeCreditsMinusDebits()
    {
        var balance = new DailyBalance
        {
            Date = new DateOnly(2026, 6, 17),
            TotalCredits = 500m,
            TotalDebits = 120m,
            UpdatedAt = DateTime.UtcNow
        };

        balance.ConsolidatedBalance.Should().Be(380m);
    }
}
