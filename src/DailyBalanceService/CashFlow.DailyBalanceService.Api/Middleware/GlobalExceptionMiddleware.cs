using System.Net;
using System.Text.Json;

namespace CashFlow.DailyBalanceService.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            logger.LogInformation("[application] Resource not found: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.NotFound, "Not found", ex.Message);
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

public class NotFoundException(string message) : Exception(message);
