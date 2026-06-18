using CashFlow.LaunchService.Api.Domain;
using CashFlow.LaunchService.Api.Middleware;
using FluentAssertions;

namespace CashFlow.LaunchService.Tests.Domain;

public class LaunchTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnLaunch()
    {
        var date = new DateOnly(2026, 6, 17);
        var launch = Launch.Create(date, 100.50m, LaunchType.Credit, "Sale revenue");

        launch.Id.Should().NotBeEmpty();
        launch.Date.Should().Be(date);
        launch.Amount.Should().Be(100.50m);
        launch.Type.Should().Be(LaunchType.Credit);
        launch.Description.Should().Be("Sale revenue");
        launch.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithNonPositiveAmount_ShouldThrowDomainException(decimal invalidAmount)
    {
        var act = () => Launch.Create(new DateOnly(2026, 6, 17), invalidAmount, LaunchType.Debit, "Test");

        act.Should().Throw<DomainException>()
            .WithMessage("*Amount must be greater than zero*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Create_WithEmptyDescription_ShouldThrowDomainException(string invalidDescription)
    {
        var act = () => Launch.Create(new DateOnly(2026, 6, 17), 100m, LaunchType.Credit, invalidDescription);

        act.Should().Throw<DomainException>()
            .WithMessage("*Description is required*");
    }

    [Fact]
    public void Create_WithDescriptionExceeding255Chars_ShouldThrowDomainException()
    {
        var longDescription = new string('x', 256);

        var act = () => Launch.Create(new DateOnly(2026, 6, 17), 100m, LaunchType.Credit, longDescription);

        act.Should().Throw<DomainException>()
            .WithMessage("*255 characters*");
    }

    [Fact]
    public void Create_ShouldTrimDescription()
    {
        var launch = Launch.Create(new DateOnly(2026, 6, 17), 100m, LaunchType.Debit, "  supplier payment  ");

        launch.Description.Should().Be("supplier payment");
    }

    [Fact]
    public void Create_WithDebitType_ShouldSetTypeCorrectly()
    {
        var launch = Launch.Create(new DateOnly(2026, 6, 17), 50m, LaunchType.Debit, "Office supplies");

        launch.Type.Should().Be(LaunchType.Debit);
    }
}
