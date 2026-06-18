using CashFlow.DailyBalanceService.Api.DTOs;
using CashFlow.DailyBalanceService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.DailyBalanceService.Api.Controllers;

/// <summary>Consulta do saldo diário consolidado.</summary>
[ApiController]
[Route("api/balance")]
[Authorize]
[Tags("Daily Balance")]
public class DailyBalanceController(DailyBalanceQueryService queryService) : ControllerBase
{
    /// <summary>Retorna o saldo consolidado de uma data específica.</summary>
    /// <remarks>
    /// O saldo é atualizado de forma **assíncrona** via eventos publicados pelo Launch Service.
    /// Pode haver um pequeno lag entre o registro do lançamento e a atualização do saldo.
    ///
    /// O `consolidatedBalance` = `totalCredits` - `totalDebits`.
    /// </remarks>
    /// <param name="date">Data no formato yyyy-MM-dd (ex: 2026-06-17)</param>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(DailyBalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByDate([FromRoute] DateOnly date, CancellationToken ct)
    {
        var result = await queryService.GetByDateAsync(date, ct);
        return Ok(result);
    }

    /// <summary>Retorna os saldos consolidados de um período.</summary>
    /// <param name="from">Data inicial no formato yyyy-MM-dd</param>
    /// <param name="to">Data final no formato yyyy-MM-dd</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DailyBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByPeriod([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { message = "'from' must be less than or equal to 'to'." });

        var result = await queryService.GetByPeriodAsync(from, to, ct);
        return Ok(result);
    }
}
