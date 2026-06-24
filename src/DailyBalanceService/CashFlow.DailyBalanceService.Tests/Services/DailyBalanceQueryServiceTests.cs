using CashFlow.DailyBalanceService.Api.Data;
using CashFlow.DailyBalanceService.Api.Domain;
using CashFlow.DailyBalanceService.Api.Middleware;
using CashFlow.DailyBalanceService.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CashFlow.DailyBalanceService.Tests.Services;

public class DailyBalanceQueryServiceTests
{
    private static BalanceDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<BalanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BalanceDbContext(options);
    }

    [Fact]
    public async Task GetByDate_WhenBalanceExists_ShouldReturnCorrectResponse()
    {
        var db = CreateInMemoryContext();
        var date = new DateOnly(2026, 6, 17);

        db.DailyBalances.Add(new DailyBalance
        {
            Date = date,
            TotalCredits = 1000m,
            TotalDebits = 300m,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new DailyBalanceQueryService(db, new MemoryCache(new MemoryCacheOptions()), NullLogger<DailyBalanceQueryService>.Instance);
        var result = await service.GetByDateAsync(date, CancellationToken.None);

        result.Date.Should().Be(date);
        result.TotalCredits.Should().Be(1000m);
        result.TotalDebits.Should().Be(300m);
        result.ConsolidatedBalance.Should().Be(700m);
    }

    [Fact]
    public async Task GetByDate_WhenBalanceDoesNotExist_ShouldThrowNotFoundException()
    {
        var db = CreateInMemoryContext();
        var service = new DailyBalanceQueryService(db, new MemoryCache(new MemoryCacheOptions()), NullLogger<DailyBalanceQueryService>.Instance);
        var date = new DateOnly(2026, 6, 17);

        var act = async () => await service.GetByDateAsync(date, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{date:yyyy-MM-dd}*");
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnOrderedBalances()
    {
        var db = CreateInMemoryContext();

        db.DailyBalances.AddRange(
            new DailyBalance { Date = new DateOnly(2026, 6, 19), TotalCredits = 300m, TotalDebits = 0m, UpdatedAt = DateTime.UtcNow },
            new DailyBalance { Date = new DateOnly(2026, 6, 17), TotalCredits = 100m, TotalDebits = 50m, UpdatedAt = DateTime.UtcNow },
            new DailyBalance { Date = new DateOnly(2026, 6, 18), TotalCredits = 200m, TotalDebits = 80m, UpdatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var service = new DailyBalanceQueryService(db, new MemoryCache(new MemoryCacheOptions()), NullLogger<DailyBalanceQueryService>.Instance);
        var result = await service.GetByPeriodAsync(
            new DateOnly(2026, 6, 17),
            new DateOnly(2026, 6, 19),
            CancellationToken.None);

        result.Should().HaveCount(3);
        result[0].Date.Should().Be(new DateOnly(2026, 6, 17));
        result[1].Date.Should().Be(new DateOnly(2026, 6, 18));
        result[2].Date.Should().Be(new DateOnly(2026, 6, 19));
    }

    [Fact]
    public async Task GetByDate_SecondCall_ShouldReturnFromCache()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var db = CreateInMemoryContext();
        var date = new DateOnly(2026, 6, 17);
        db.DailyBalances.Add(new DailyBalance
        {
            Date = date,
            TotalCredits = 50m,
            TotalDebits = 10m,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new DailyBalanceQueryService(db, cache, NullLogger<DailyBalanceQueryService>.Instance);
        var first = await service.GetByDateAsync(date, CancellationToken.None);
        var second = await service.GetByDateAsync(date, CancellationToken.None);
        second.Should().BeEquivalentTo(first);
    }

    [Fact]
    public async Task GetByPeriod_ShouldReturnEmptyListWhenNoData()
    {
        var db = CreateInMemoryContext();
        var service = new DailyBalanceQueryService(db, new MemoryCache(new MemoryCacheOptions()), NullLogger<DailyBalanceQueryService>.Instance);

        var result = await service.GetByPeriodAsync(
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            CancellationToken.None);

        result.Should().BeEmpty();
    }
}
