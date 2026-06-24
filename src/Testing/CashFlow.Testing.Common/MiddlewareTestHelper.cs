using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CashFlow.Testing.Common;

public static class MiddlewareTestHelper
{
    public static async Task InvokeMiddleware<TMiddleware>(
        TMiddleware middleware,
        HttpContext context,
        RequestDelegate? next = null)
        where TMiddleware : class
    {
        var invoke = typeof(TMiddleware).GetMethod("InvokeAsync")!;
        var parameters = invoke.GetParameters();
        object?[] args = parameters.Length switch
        {
            1 => [context],
            2 => [context, next ?? (_ => Task.CompletedTask)],
            _ => throw new InvalidOperationException($"Unexpected InvokeAsync signature on {typeof(TMiddleware).Name}")
        };
        await (Task)invoke.Invoke(middleware, args)!;
    }

    public static DefaultHttpContext CreateContext(string method = "GET", string path = "/")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    public static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    public static ILogger<T> CreateNullLogger<T>() => NullLogger<T>.Instance;
}
