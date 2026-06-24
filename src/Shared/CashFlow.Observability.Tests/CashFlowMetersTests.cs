using CashFlow.Observability;
using FluentAssertions;

namespace CashFlow.Observability.Tests;

public class CashFlowMetersTests
{
    [Fact]
    public void AllCounters_ShouldBeRecordable()
    {
        CashFlowMeters.MeterName.Should().Be("CashFlow.Platform");

        CashFlowMeters.LaunchRegistrations.Add(1);
        CashFlowMeters.LaunchValidationErrors.Add(1);
        CashFlowMeters.BalanceQueries.Add(1);
        CashFlowMeters.WorkerConsolidations.Add(1);
        CashFlowMeters.WorkerConsolidationErrors.Add(1);
    }
}
