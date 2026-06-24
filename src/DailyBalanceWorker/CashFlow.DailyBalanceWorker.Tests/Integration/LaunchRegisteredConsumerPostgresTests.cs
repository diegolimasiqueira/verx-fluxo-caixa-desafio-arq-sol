using CashFlow.DailyBalanceWorker.Consumers;
using CashFlow.DailyBalanceWorker.Data;
using CashFlow.DailyBalanceWorker.Domain;
using CashFlow.LaunchService.Api.Domain.Events;
using CashFlow.Testing.Common;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CashFlow.DailyBalanceWorker.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class LaunchRegisteredConsumerPostgresTests
{
    private readonly PostgresFixture _postgres;

    public LaunchRegisteredConsumerPostgresTests(PostgresFixture postgres) => _postgres = postgres;

    [Fact]
    public async Task Consume_WithPostgres_ShouldPersistBalance()
    {
        var options = new DbContextOptionsBuilder<WorkerDbContext>()
            .UseNpgsql(_postgres.GetConnectionString("daily_balance_db"))
            .Options;

        await using var db = new WorkerDbContext(options);
        await db.Database.EnsureCreatedAsync();

        var consumer = new LaunchRegisteredConsumer(db, NullLogger<LaunchRegisteredConsumer>.Instance);
        var evt = new LaunchRegisteredEvent
        {
            LaunchId = Guid.NewGuid(),
            Date = new DateOnly(2026, 6, 20),
            Amount = 75m,
            Type = "credit",
            CreatedAt = DateTime.UtcNow
        };

        var context = Substitute.For<ConsumeContext<LaunchRegisteredEvent>>();
        context.Message.Returns(evt);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        var balance = await db.DailyBalances.SingleAsync(b => b.Date == evt.Date);
        balance.TotalCredits.Should().Be(75m);
        balance.ConsolidatedBalance.Should().Be(75m);
    }
}

[Collection(PostgresCollection.Name)]
public class DailyBalanceDomainTests
{
    [Fact]
    public void ConsolidatedBalance_ShouldCalculate()
    {
        var balance = new DailyBalance
        {
            Date = new DateOnly(2026, 1, 1),
            TotalCredits = 10m,
            TotalDebits = 3m
        };

        balance.ConsolidatedBalance.Should().Be(7m);
    }
}
