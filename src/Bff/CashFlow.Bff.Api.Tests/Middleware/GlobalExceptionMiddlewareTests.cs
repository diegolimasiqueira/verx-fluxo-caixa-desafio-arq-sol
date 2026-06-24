using System.Net;
using CashFlow.Bff.Api.Middleware;
using CashFlow.Testing.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace CashFlow.Bff.Api.Tests.Middleware;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithDomainException_ShouldReturn422()
    {
        var middleware = new GlobalExceptionMiddleware(_ => throw new DomainException("rule"), NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = MiddlewareTestHelper.CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task InvokeAsync_WithNotFoundException_ShouldReturn404()
    {
        var middleware = new GlobalExceptionMiddleware(_ => throw new NotFoundException("missing"), NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = MiddlewareTestHelper.CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvokeAsync_WithDownstreamException_ShouldReturnCustomStatus()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new DownstreamException(HttpStatusCode.BadGateway, "bad gateway", "down"),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = MiddlewareTestHelper.CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task InvokeAsync_WithUnhandledException_ShouldReturn500()
    {
        var middleware = new GlobalExceptionMiddleware(_ => throw new InvalidOperationException(), NullLogger<GlobalExceptionMiddleware>.Instance);
        var context = MiddlewareTestHelper.CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public void DownstreamException_ShouldExposeProperties()
    {
        var ex = new DownstreamException(HttpStatusCode.BadGateway, "title", "detail");
        ex.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        ex.Title.Should().Be("title");
        ex.Detail.Should().Be("detail");
    }
}
