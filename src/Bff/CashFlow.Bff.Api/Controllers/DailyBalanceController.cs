using CashFlow.Bff.Api.Domain;
using CashFlow.Bff.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.Bff.Api.Controllers;

/// <summary>Proxy para o Daily Balance Service — saldo diário consolidado.</summary>
[ApiController]
[Route("api/balance")]
[Authorize(Roles = Roles.AdminOrMerchant)]
[Tags("Daily Balance")]
public class DailyBalanceController(DownstreamProxy proxy) : ControllerBase
{
    [HttpGet("{date}")]
    public Task<IActionResult> GetByDate(CancellationToken ct) =>
        proxy.ForwardAsync(HttpContext, "balance", ct);

    [HttpGet]
    public Task<IActionResult> GetByPeriod(CancellationToken ct) =>
        proxy.ForwardAsync(HttpContext, "balance", ct);
}
