using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CashFlow.Bff.Api.Middleware;
using CashFlow.Bff.Api.Services;
using CashFlow.Testing.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CashFlow.Bff.Api.Tests.Services;

public class DownstreamProxyTests
{
    [Fact]
    public async Task ForwardAsync_ShouldProxyResponseAndAuthorization()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"id\":1}]", Encoding.UTF8, "application/json")
            });

        var services = new ServiceCollection();
        services.AddHttpClient("launch", c => c.BaseAddress = new Uri("http://downstream"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var proxy = new DownstreamProxy(factory);

        var context = MiddlewareTestHelper.CreateContext("GET", "/api/launches");
        context.Request.QueryString = new QueryString("?date=2026-06-17");
        context.Request.Headers.Authorization = "Bearer token-123";

        var result = await proxy.ForwardAsync(context, "launch", CancellationToken.None);

        var content = result as ContentResult;
        content!.StatusCode.Should().Be(200);
        content.Content.Should().Contain("id");
        handler.Requests[0].Headers.Authorization!.Scheme.Should().Be("Bearer");
    }

    [Fact]
    public async Task ForwardAsync_WithPostBody_ShouldForwardContent()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Created) { Content = new StringContent("{}") });

        var services = new ServiceCollection();
        services.AddHttpClient("launch", c => c.BaseAddress = new Uri("http://downstream"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var proxy = new DownstreamProxy(factory);

        var context = MiddlewareTestHelper.CreateContext("POST", "/api/launches");
        var body = "{\"amount\":1}";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        context.Request.ContentLength = body.Length;
        context.Request.ContentType = "application/json";

        await proxy.ForwardAsync(context, "launch", CancellationToken.None);

        handler.Requests[0].Content.Should().NotBeNull();
    }

    [Fact]
    public async Task ForwardAsync_WithoutAuthorization_ShouldStillForward()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ok") });

        var services = new ServiceCollection();
        services.AddHttpClient("balance", c => c.BaseAddress = new Uri("http://downstream"))
            .ConfigurePrimaryHttpMessageHandler(() => handler);
        var factory = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var proxy = new DownstreamProxy(factory);

        var context = MiddlewareTestHelper.CreateContext("GET", "/api/balance/2026-06-17");
        var result = await proxy.ForwardAsync(context, "balance", CancellationToken.None);

        (result as ContentResult)!.Content.Should().Be("ok");
        handler.Requests[0].Headers.Authorization.Should().BeNull();
    }
}
