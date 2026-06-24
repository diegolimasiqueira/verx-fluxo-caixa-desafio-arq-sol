using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CashFlow.DailyBalanceService.Api.Data;
using CashFlow.DailyBalanceService.Api.Domain;
using CashFlow.Testing.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.DailyBalanceService.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class BalanceApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private WebApplicationFactory<Program>? _factory;
    private HttpClient? _client;

    public BalanceApiIntegrationTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        var conn = _postgres.GetConnectionString("daily_balance_db");
        var options = new DbContextOptionsBuilder<BalanceDbContext>().UseNpgsql(conn).Options;
        await using (var db = new BalanceDbContext(options))
        {
            await db.Database.MigrateAsync();
            var date = new DateOnly(2026, 6, 17);
            if (!await db.DailyBalances.AnyAsync(b => b.Date == date))
            {
                db.DailyBalances.Add(new DailyBalance
                {
                    Date = date,
                    TotalCredits = 1000m,
                    TotalDebits = 200m,
                    UpdatedAt = DateTime.UtcNow
                });
                await db.SaveChangesAsync();
            }
        }

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Default", conn);
                builder.UseSetting("Jwt:Secret", JwtTestHelper.Secret);
                builder.UseSetting("Jwt:Issuer", JwtTestHelper.Issuer);
                builder.UseSetting("Jwt:Audience", JwtTestHelper.Audience);
            });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken());
    }

    public async Task DisposeAsync()
    {
        _client?.Dispose();
        if (_factory is not null)
            await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetByDate_ShouldReturnBalance()
    {
        var balance = await _client.GetFromJsonAsync<BalanceDto>("/api/balance/2026-06-17");

        balance!.ConsolidatedBalance.Should().Be(800m);
    }

    [Fact]
    public async Task GetByPeriod_WithValidRange_ShouldReturnBalances()
    {
        var list = await _client.GetFromJsonAsync<List<BalanceDto>>(
            "/api/balance?from=2026-06-17&to=2026-06-17");

        list!.Should().ContainSingle(b => b.ConsolidatedBalance == 800m);
    }

    [Fact]
    public async Task GetByPeriod_WithInvalidRange_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/balance?from=2026-06-20&to=2026-06-17");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByDate_WhenMissing_ShouldReturn404()
    {
        var response = await _client.GetAsync("/api/balance/2099-01-01");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record BalanceDto(DateOnly Date, decimal TotalCredits, decimal TotalDebits, decimal ConsolidatedBalance, DateTime UpdatedAt);
}
