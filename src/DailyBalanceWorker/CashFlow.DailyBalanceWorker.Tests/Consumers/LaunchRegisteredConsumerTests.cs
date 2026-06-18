using CashFlow.DailyBalanceWorker.Consumers;
using CashFlow.DailyBalanceWorker.Data;
using CashFlow.LaunchService.Api.Domain.Events;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CashFlow.DailyBalanceWorker.Tests.Consumers;

public class LaunchRegisteredConsumerTests
{
    private static WorkerDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<WorkerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new WorkerDbContext(options);
    }

    [Fact]
    public async Task Consume_CreditEvent_ShouldIncreaseTotalCredits()
    {
        var db = CreateInMemoryContext();
        var consumer = new LaunchRegisteredConsumer(db, NullLogger<LaunchRegisteredConsumer>.Instance);
        var date = new DateOnly(2026, 6, 17);

        var evt = new LaunchRegisteredEvent
        {
            LaunchId = Guid.NewGuid(),
            Date = date,
            Amount = 200m,
            Type = "credit",
            CreatedAt = DateTime.UtcNow
        };

        var context = Substitute.For<ConsumeContext<LaunchRegisteredEvent>>();
        context.Message.Returns(evt);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        var balance = await db.DailyBalances.FirstAsync(b => b.Date == date);
        balance.TotalCredits.Should().Be(200m);
        balance.TotalDebits.Should().Be(0m);
        balance.ConsolidatedBalance.Should().Be(200m);
    }

    [Fact]
    public async Task Consume_DebitEvent_ShouldIncreaseTotalDebits()
    {
        var db = CreateInMemoryContext();
        var consumer = new LaunchRegisteredConsumer(db, NullLogger<LaunchRegisteredConsumer>.Instance);
        var date = new DateOnly(2026, 6, 17);

        var evt = new LaunchRegisteredEvent
        {
            LaunchId = Guid.NewGuid(),
            Date = date,
            Amount = 50m,
            Type = "debit",
            CreatedAt = DateTime.UtcNow
        };

        var context = Substitute.For<ConsumeContext<LaunchRegisteredEvent>>();
        context.Message.Returns(evt);
        context.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(context);

        var balance = await db.DailyBalances.FirstAsync(b => b.Date == date);
        balance.TotalCredits.Should().Be(0m);
        balance.TotalDebits.Should().Be(50m);
        balance.ConsolidatedBalance.Should().Be(-50m);
    }

    [Fact]
    public async Task Consume_MultipleEvents_ShouldAccumulateCorrectly()
    {
        var db = CreateInMemoryContext();
        var consumer = new LaunchRegisteredConsumer(db, NullLogger<LaunchRegisteredConsumer>.Instance);
        var date = new DateOnly(2026, 6, 17);

        var creditContext = Substitute.For<ConsumeContext<LaunchRegisteredEvent>>();
        creditContext.Message.Returns(new LaunchRegisteredEvent
        {
            LaunchId = Guid.NewGuid(), Date = date, Amount = 500m, Type = "credit", CreatedAt = DateTime.UtcNow
        });
        creditContext.CancellationToken.Returns(CancellationToken.None);

        var debitContext = Substitute.For<ConsumeContext<LaunchRegisteredEvent>>();
        debitContext.Message.Returns(new LaunchRegisteredEvent
        {
            LaunchId = Guid.NewGuid(), Date = date, Amount = 150m, Type = "debit", CreatedAt = DateTime.UtcNow
        });
        debitContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(creditContext);
        await consumer.Consume(debitContext);

        var balance = await db.DailyBalances.FirstAsync(b => b.Date == date);
        balance.TotalCredits.Should().Be(500m);
        balance.TotalDebits.Should().Be(150m);
        balance.ConsolidatedBalance.Should().Be(350m);
    }

    [Fact]
    public async Task Consume_SecondEventForSameDate_ShouldUpdateExistingRecord()
    {
        var db = CreateInMemoryContext();
        var consumer = new LaunchRegisteredConsumer(db, NullLogger<LaunchRegisteredConsumer>.Instance);
        var date = new DateOnly(2026, 6, 17);

        for (var i = 0; i < 2; i++)
        {
            var ctx = Substitute.For<ConsumeContext<LaunchRegisteredEvent>>();
            ctx.Message.Returns(new LaunchRegisteredEvent
            {
                LaunchId = Guid.NewGuid(), Date = date, Amount = 100m, Type = "credit", CreatedAt = DateTime.UtcNow
            });
            ctx.CancellationToken.Returns(CancellationToken.None);
            await consumer.Consume(ctx);
        }

        var count = await db.DailyBalances.CountAsync(b => b.Date == date);
        count.Should().Be(1);

        var balance = await db.DailyBalances.FirstAsync(b => b.Date == date);
        balance.TotalCredits.Should().Be(200m);
    }
}
