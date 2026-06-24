using CashFlow.Bff.Api.Services;
using FluentAssertions;

namespace CashFlow.Bff.Api.Tests.Services;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_And_Verify_ShouldMatch()
    {
        var hash = PasswordHasher.Hash("Secret@123");
        PasswordHasher.Verify("Secret@123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithWrongPassword_ShouldReturnFalse()
    {
        var hash = PasswordHasher.Hash("Secret@123");
        PasswordHasher.Verify("wrong", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_WithInvalidFormat_ShouldReturnFalse()
    {
        PasswordHasher.Verify("pwd", "invalid").Should().BeFalse();
    }
}
