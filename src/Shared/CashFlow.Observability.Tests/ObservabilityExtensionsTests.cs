using CashFlow.Observability;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace CashFlow.Observability.Tests;

public class ObservabilityExtensionsTests
{
    [Fact]
    public void AddCashFlowObservability_WhenTesting_ShouldSkipRegistration()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Testing";

        var result = builder.AddCashFlowObservability("test-service");

        result.Should().BeSameAs(builder);
    }

    [Fact]
    public async Task AddCashFlowObservability_WhenProduction_ShouldRegisterTelemetry()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://127.0.0.1:4317");
        try
        {
            var builder = WebApplication.CreateBuilder();
            builder.Environment.EnvironmentName = Environments.Production;
            builder.AddCashFlowObservability("obs-test");

            await using var app = builder.Build();
            app.UseCashFlowObservability().Should().BeSameAs(app);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        }
    }
}
