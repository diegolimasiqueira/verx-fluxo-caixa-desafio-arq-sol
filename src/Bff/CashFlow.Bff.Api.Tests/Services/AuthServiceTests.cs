using CashFlow.Bff.Api.Data;
using CashFlow.Bff.Api.Domain;
using CashFlow.Bff.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace CashFlow.Bff.Api.Tests.Services;

public class AuthServiceTests
{
    private static AuthService CreateService(BffDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtTestConfig.Secret,
                ["Jwt:Issuer"] = JwtTestConfig.Issuer,
                ["Jwt:Audience"] = JwtTestConfig.Audience
            })
            .Build();
        return new AuthService(db, config);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ShouldReturnToken()
    {
        await using var db = BffTestDb.CreateContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "User",
            Email = "user@test.com",
            PasswordHash = PasswordHasher.Hash("Pass@123"),
            Role = Roles.Merchant,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var token = await service.AuthenticateAsync("user@test.com", "Pass@123");

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ShouldReturnNull()
    {
        await using var db = BffTestDb.CreateContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "User",
            Email = "user@test.com",
            PasswordHash = PasswordHasher.Hash("Pass@123"),
            Role = Roles.Merchant,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var token = await service.AuthenticateAsync("user@test.com", "wrong");

        token.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithUnknownEmail_ShouldReturnNull()
    {
        await using var db = BffTestDb.CreateContext();
        var service = CreateService(db);

        var token = await service.AuthenticateAsync("missing@test.com", "Pass@123");

        token.Should().BeNull();
    }
}

internal static class JwtTestConfig
{
    public const string Secret = CashFlow.Testing.Common.JwtTestHelper.Secret;
    public const string Issuer = CashFlow.Testing.Common.JwtTestHelper.Issuer;
    public const string Audience = CashFlow.Testing.Common.JwtTestHelper.Audience;
}
