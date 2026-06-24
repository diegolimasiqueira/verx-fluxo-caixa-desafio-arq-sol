using System.Net;
using CashFlow.DailyBalanceService.Api.Middleware;
using CashFlow.Testing.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CashFlow.DailyBalanceService.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithNotFoundException_ShouldReturn404()
    {
        var middleware = new GlobalExceptionMiddleware(_ => throw new NotFoundException("missing"), NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = MiddlewareTestHelper.CreateContext();
        await middleware.InvokeAsync(context);
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_ShouldReturn500()
    {
        var middleware = new GlobalExceptionMiddleware(_ => throw new Exception(), NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = MiddlewareTestHelper.CreateContext();
        await middleware.InvokeAsync(context);
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }
}
