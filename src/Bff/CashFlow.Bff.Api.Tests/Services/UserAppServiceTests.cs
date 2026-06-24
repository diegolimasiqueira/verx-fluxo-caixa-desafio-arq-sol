using CashFlow.Bff.Api.Data;
using CashFlow.Bff.Api.Domain;
using CashFlow.Bff.Api.DTOs;
using CashFlow.Bff.Api.Middleware;
using CashFlow.Bff.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Bff.Api.Tests.Services;

public class UserAppServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistMerchantUser()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);

        var created = await service.CreateAsync(new CreateUserRequest("Merchant", "m@test.com", "Pass@123"));

        created.Email.Should().Be("m@test.com");
        created.Role.Should().Be(Roles.Merchant);
        (await db.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ShouldThrowDomainException()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);
        await service.CreateAsync(new CreateUserRequest("One", "dup@test.com", "Pass@123"));

        var act = async () => await service.CreateAsync(new CreateUserRequest("Two", "dup@test.com", "Pass@456"));

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ShouldThrowNotFoundException()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);

        var act = async () => await service.UpdateAsync(Guid.NewGuid(), new UpdateUserRequest("X", "x@test.com"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdatePasswordAsync_WhenMissing_ShouldThrowNotFoundException()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);

        var act = async () => await service.UpdatePasswordAsync(Guid.NewGuid(), new UpdatePasswordRequest("Pass@123"));

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ShouldThrowNotFoundException()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);

        var act = async () => await service.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateUser()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);
        var created = await service.CreateAsync(new CreateUserRequest("Old", "old@test.com", "Pass@123"));

        var updated = await service.UpdateAsync(created.Id, new UpdateUserRequest("New", "new@test.com"));

        updated.Name.Should().Be("New");
        updated.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateEmail_ShouldThrowDomainException()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);
        await service.CreateAsync(new CreateUserRequest("A", "a@test.com", "Pass@123"));
        var b = await service.CreateAsync(new CreateUserRequest("B", "b@test.com", "Pass@123"));

        var act = async () => await service.UpdateAsync(b.Id, new UpdateUserRequest("B", "a@test.com"));

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task UpdatePasswordAsync_ShouldChangePasswordHash()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);
        var user = await service.CreateAsync(new CreateUserRequest("User", "u@test.com", "OldPass1"));
        var oldHash = (await db.Users.FindAsync(user.Id))!.PasswordHash;

        await service.UpdatePasswordAsync(user.Id, new UpdatePasswordRequest("NewPass2"));

        var newHash = (await db.Users.FindAsync(user.Id))!.PasswordHash;
        newHash.Should().NotBe(oldHash);
        PasswordHasher.Verify("NewPass2", newHash).Should().BeTrue();
    }

    [Fact]
    public async Task ListAsync_ShouldReturnOrderedUsers()
    {
        await using var db = BffTestDb.CreateContext();
        var service = new UserAppService(db);
        await service.CreateAsync(new CreateUserRequest("Zeta", "z@test.com", "Pass@123"));
        await service.CreateAsync(new CreateUserRequest("Alpha", "a@test.com", "Pass@123"));

        var list = await service.ListAsync();

        list.Should().HaveCount(2);
        list[0].Name.Should().Be("Alpha");
    }
}
