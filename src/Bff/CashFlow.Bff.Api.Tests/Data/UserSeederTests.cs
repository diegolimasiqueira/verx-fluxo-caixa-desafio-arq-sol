using CashFlow.Bff.Api.Data;
using CashFlow.Bff.Api.Domain;
using CashFlow.Bff.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Bff.Api.Tests.Data;

public class UserSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenAdminMissing_ShouldCreateAdmin()
    {
        await using var db = BffTestDb.CreateContext();

        await UserSeeder.SeedAsync(db);

        var admin = await db.Users.SingleAsync(u => u.Email == UserSeeder.DefaultAdminEmail);
        admin.Role.Should().Be(Roles.Admin);
        PasswordHasher.Verify(UserSeeder.DefaultAdminPassword, admin.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task SeedAsync_WhenAdminExistsWithWrongRole_ShouldFixRole()
    {
        await using var db = BffTestDb.CreateContext();
        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Name = "Admin",
            Email = UserSeeder.DefaultAdminEmail,
            PasswordHash = PasswordHasher.Hash("x"),
            Role = Roles.Merchant,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await UserSeeder.SeedAsync(db);

        var admin = await db.Users.SingleAsync(u => u.Email == UserSeeder.DefaultAdminEmail);
        admin.Role.Should().Be(Roles.Admin);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminAlreadyCorrect_ShouldNotDuplicate()
    {
        await using var db = BffTestDb.CreateContext();
        await UserSeeder.SeedAsync(db);
        await UserSeeder.SeedAsync(db);

        (await db.Users.CountAsync()).Should().Be(1);
    }
}
