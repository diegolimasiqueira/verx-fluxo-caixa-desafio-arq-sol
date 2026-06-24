using System.Net;
using System.Text.Json;

namespace CashFlow.Bff.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("[business] Domain rule violation: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.UnprocessableEntity, "Business rule violation", ex.Message);
        }
        catch (NotFoundException ex)
        {
            logger.LogInformation("[application] Resource not found: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.NotFound, "Not found", ex.Message);
        }
        catch (DownstreamException ex)
        {
            logger.LogWarning("[application] Downstream service error: {Message}", ex.Message);
            await WriteErrorResponse(context, ex.StatusCode, ex.Title, ex.Detail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[application] Unhandled exception");
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "Internal server error", "An unexpected error occurred.");
        }
    }

    private static Task WriteErrorResponse(HttpContext context, HttpStatusCode status, string title, string detail)
    {
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/problem+json";

        var problem = new { title, detail, status = (int)status };
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }
}

public class DomainException(string message) : Exception(message);
public class NotFoundException(string message) : Exception(message);

public class DownstreamException(HttpStatusCode statusCode, string title, string detail)
    : Exception(detail)
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string Title { get; } = title;
    public string Detail { get; } = detail;
}
