using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Bff.Api.Services;

public class DownstreamProxy(IHttpClientFactory httpClientFactory)
{
    public async Task<IActionResult> ForwardAsync(HttpContext context, string clientName, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(clientName);
        var path = context.Request.Path.Value!.TrimStart('/');
        var targetUri = path + context.Request.QueryString;

        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUri);

        if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Content-Type"))
        {
            request.Content = new StreamContent(context.Request.Body);
            if (context.Request.ContentType is not null)
                request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
        }

        if (context.Request.Headers.Authorization.FirstOrDefault() is string auth)
            request.Headers.TryAddWithoutValidation("Authorization", auth);

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        var body = await response.Content.ReadAsStringAsync(ct);

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            Content = body,
            ContentType = contentType
        };
    }
}
