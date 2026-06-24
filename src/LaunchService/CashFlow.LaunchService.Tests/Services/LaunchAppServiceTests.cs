using CashFlow.LaunchService.Api.Data;
using CashFlow.LaunchService.Api.Domain;
using CashFlow.LaunchService.Api.Domain.Events;
using CashFlow.LaunchService.Api.DTOs;
using CashFlow.LaunchService.Api.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CashFlow.LaunchService.Tests.Services;

public class LaunchAppServiceTests
{
    private static LaunchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LaunchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LaunchDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_ShouldPersistAndPublish()
    {
        await using var db = CreateContext();
        var publisher = Substitute.For<IPublishEndpoint>();
        var service = new LaunchAppService(db, publisher, NullLogger<LaunchAppService>.Instance);

        var response = await service.RegisterAsync(
            new RegisterLaunchRequest(new DateOnly(2026, 6, 17), 100m, "credit", "Sale"),
            CancellationToken.None);

        response.Amount.Should().Be(100m);
        (await db.Launches.CountAsync()).Should().Be(1);
        await publisher.Received(1).Publish(Arg.Any<LaunchRegisteredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByDateAsync_ShouldReturnLaunches()
    {
        await using var db = CreateContext();
        db.Launches.Add(Launch.Create(new DateOnly(2026, 6, 17), 50m, LaunchType.Debit, "x"));
        await db.SaveChangesAsync();

        var service = new LaunchAppService(db, Substitute.For<IPublishEndpoint>(), NullLogger<LaunchAppService>.Instance);
        var result = await service.GetByDateAsync(new DateOnly(2026, 6, 17), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Type.Should().Be("debit");
    }

    [Fact]
    public async Task GetByPeriodAsync_ShouldReturnOrderedLaunches()
    {
        await using var db = CreateContext();
        db.Launches.Add(Launch.Create(new DateOnly(2026, 6, 18), 10m, LaunchType.Credit, "b"));
        db.Launches.Add(Launch.Create(new DateOnly(2026, 6, 17), 20m, LaunchType.Credit, "a"));
        await db.SaveChangesAsync();

        var service = new LaunchAppService(db, Substitute.For<IPublishEndpoint>(), NullLogger<LaunchAppService>.Instance);
        var result = await service.GetByPeriodAsync(new DateOnly(2026, 6, 17), new DateOnly(2026, 6, 18), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].Date.Should().Be(new DateOnly(2026, 6, 17));
    }
}
