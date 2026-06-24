using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CashFlow.Testing.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace CashFlow.LaunchService.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class LaunchApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public LaunchApiIntegrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString("launch_db"));
                builder.UseSetting("Jwt:Secret", JwtTestHelper.Secret);
                builder.UseSetting("Jwt:Issuer", JwtTestHelper.Issuer);
                builder.UseSetting("Jwt:Audience", JwtTestHelper.Audience);
            });

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken());
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Register_And_Query_ShouldPersistLaunch()
    {
        var create = await _client.PostAsJsonAsync("/api/launches", new
        {
            date = "2026-06-17",
            amount = 150.00,
            type = "credit",
            description = "Integration sale"
        });

        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<List<LaunchDto>>("/api/launches?date=2026-06-17");
        list!.Should().ContainSingle(l => l.Description == "Integration sale");
    }

    [Fact]
    public async Task GetByPeriod_WithValidRange_ShouldReturnLaunches()
    {
        await _client.PostAsJsonAsync("/api/launches", new
        {
            date = "2026-06-17",
            amount = 100m,
            type = "credit",
            description = "period test"
        });

        var list = await _client.GetFromJsonAsync<List<LaunchDto>>(
            "/api/launches/period?from=2026-06-17&to=2026-06-17");

        list!.Should().ContainSingle(l => l.Description == "period test");
    }

    [Fact]
    public async Task GetByPeriod_WithInvalidRange_ShouldReturn400()
    {
        var response = await _client.GetAsync("/api/launches/period?from=2026-06-20&to=2026-06-17");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithoutToken_ShouldReturn401()
    {
        using var anon = _factory.CreateClient();
        var response = await anon.PostAsJsonAsync("/api/launches", new
        {
            date = "2026-06-17",
            amount = 1,
            type = "credit",
            description = "x"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LaunchDto(Guid Id, DateOnly Date, decimal Amount, string Type, string Description, DateTime CreatedAt);
}
