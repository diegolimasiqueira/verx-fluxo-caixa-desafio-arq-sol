using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CashFlow.Testing.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace CashFlow.Bff.Api.Tests.Integration;

[Collection(PostgresCollection.Name)]
public class BffApiIntegrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private WireMockServer _wireMock = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public BffApiIntegrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync()
    {
        _wireMock = WireMockServer.Start();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString("bff_db"));
                builder.UseSetting("Jwt:Secret", JwtTestHelper.Secret);
                builder.UseSetting("Jwt:Issuer", JwtTestHelper.Issuer);
                builder.UseSetting("Jwt:Audience", JwtTestHelper.Audience);
                builder.UseSetting("Services:Launch", _wireMock.Url);
                builder.UseSetting("Services:Balance", _wireMock.Url);
            });

        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        _wireMock.Stop();
        _wireMock.Dispose();
    }

    [Fact]
    public async Task Login_WithDefaultAdmin_ShouldReturnToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@admin.com",
            password = "Master@123"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        body!.AccessToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@admin.com",
            password = "wrong"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Users_AsAdmin_ShouldManageUsers()
    {
        var token = JwtTestHelper.CreateToken("admin@admin.com", "admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "Merchant",
            email = "merchant@test.com",
            password = "Pass@123"
        });

        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await _client.GetFromJsonAsync<List<UserDto>>("/api/users");
        list!.Should().Contain(u => u.Email == "merchant@test.com");
    }

    [Fact]
    public async Task Users_AsMerchant_ShouldReturn403()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken(role: "merchant"));

        var response = await _client.GetAsync("/api/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Users_UpdateAndPassword_ShouldWork()
    {
        var token = JwtTestHelper.CreateToken("admin@admin.com", "admin");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var create = await _client.PostAsJsonAsync("/api/users", new
        {
            name = "ToUpdate",
            email = "update@test.com",
            password = "Pass@123"
        });
        var created = await create.Content.ReadFromJsonAsync<UserDto>();

        var update = await _client.PutAsJsonAsync($"/api/users/{created!.Id}", new
        {
            name = "Updated",
            email = "updated@test.com"
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var pwd = await _client.PutAsJsonAsync($"/api/users/{created.Id}/password", new { password = "NewPass1" });
        pwd.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var get = await _client.GetFromJsonAsync<UserDto>($"/api/users/{created.Id}");
        get!.Email.Should().Be("updated@test.com");
    }

    [Fact]
    public async Task Launches_ShouldProxyToDownstream()
    {
        _wireMock
            .Given(Request.Create().WithPath("/api/launches").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("[]"));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken());

        var response = await _client.GetAsync("/api/launches?date=2026-06-17");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _wireMock.LogEntries.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Balance_ShouldProxyToDownstream()
    {
        _wireMock
            .Given(Request.Create().WithPath("/api/balance/2026-06-17").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("{}"));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken());

        var response = await _client.GetAsync("/api/balance/2026-06-17");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Launches_RegisterAndPeriod_ShouldProxyToDownstream()
    {
        _wireMock
            .Given(Request.Create().WithPath("/api/launches").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(201).WithBody("{}"));
        _wireMock
            .Given(Request.Create().WithPath("/api/launches/period").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("[]"));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken());

        var create = await _client.PostAsJsonAsync("/api/launches", new
        {
            date = "2026-06-17",
            amount = 10m,
            type = "credit",
            description = "proxy"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var period = await _client.GetAsync("/api/launches/period?from=2026-06-17&to=2026-06-18");
        period.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Balance_Period_ShouldProxyToDownstream()
    {
        _wireMock
            .Given(Request.Create().WithPath("/api/balance").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBody("[]"));

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", JwtTestHelper.CreateToken());

        var response = await _client.GetAsync("/api/balance?from=2026-06-17&to=2026-06-18");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ShouldReturn401()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.GetAsync("/api/launches?date=2026-06-17");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginResponseDto(string AccessToken, string TokenType, int ExpiresIn);
    private sealed record UserDto(Guid Id, string Name, string Email, string Role, DateTime CreatedAt, DateTime UpdatedAt);
}
