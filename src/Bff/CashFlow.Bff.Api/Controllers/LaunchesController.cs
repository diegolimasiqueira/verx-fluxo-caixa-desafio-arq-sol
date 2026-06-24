using CashFlow.Bff.Api.Domain;
using CashFlow.Bff.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Bff.Api.Controllers;

/// <summary>Proxy para o Launch Service — lançamentos financeiros.</summary>
[ApiController]
[Route("api/launches")]
[Authorize(Roles = Roles.AdminOrMerchant)]
[Tags("Launches")]
public class LaunchesController(DownstreamProxy proxy) : ControllerBase
{
    [HttpPost]
    public Task<IActionResult> Register(CancellationToken ct) =>
        proxy.ForwardAsync(HttpContext, "launch", ct);

    [HttpGet]
    public Task<IActionResult> GetByDate(CancellationToken ct) =>
        proxy.ForwardAsync(HttpContext, "launch", ct);

    [HttpGet("period")]
    public Task<IActionResult> GetByPeriod(CancellationToken ct) =>
        proxy.ForwardAsync(HttpContext, "launch", ct);
}
