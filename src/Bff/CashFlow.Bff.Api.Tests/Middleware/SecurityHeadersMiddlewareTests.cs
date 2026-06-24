using CashFlow.Bff.Api.Middleware;
using CashFlow.Testing.Common;
using FluentAssertions;

namespace CashFlow.Bff.Api.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldAddSecurityHeaders()
    {
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);
        var context = MiddlewareTestHelper.CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        context.Response.Headers["X-Frame-Options"].ToString().Should().Be("DENY");
        context.Response.Headers.ContainsKey("Content-Security-Policy").Should().BeTrue();
    }
}
