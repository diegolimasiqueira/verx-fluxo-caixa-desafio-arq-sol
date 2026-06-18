using CashFlow.LaunchService.Api.DTOs;
using CashFlow.LaunchService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashFlow.LaunchService.Api.Controllers;

/// <summary>Gerenciamento de lançamentos financeiros (débitos e créditos).</summary>
[ApiController]
[Route("api/launches")]
[Authorize]
[Tags("Launches")]
public class LaunchesController(LaunchAppService launchService) : ControllerBase
{
    /// <summary>Registra um novo lançamento financeiro.</summary>
    /// <remarks>
    /// Registra um débito ou crédito para a data informada.
    /// Após o registro, um evento é publicado no broker para atualização assíncrona do saldo consolidado.
    /// 
    /// **Tipos aceitos:**
    /// - `credit` — entrada de dinheiro (crédito)
    /// - `debit`  — saída de dinheiro (débito)
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(LaunchResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Register([FromBody] RegisterLaunchRequest request, CancellationToken ct)
    {
        var response = await launchService.RegisterAsync(request, ct);
        return CreatedAtAction(nameof(GetByDate), new { date = response.Date.ToString("yyyy-MM-dd") }, response);
    }

    /// <summary>Lista lançamentos de uma data específica.</summary>
    /// <param name="date">Data no formato yyyy-MM-dd (ex: 2026-06-17)</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<LaunchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByDate([FromQuery] DateOnly date, CancellationToken ct)
    {
        var result = await launchService.GetByDateAsync(date, ct);
        return Ok(result);
    }

    /// <summary>Lista lançamentos de um período.</summary>
    /// <param name="from">Data inicial no formato yyyy-MM-dd</param>
    /// <param name="to">Data final no formato yyyy-MM-dd</param>
    [HttpGet("period")]
    [ProducesResponseType(typeof(IReadOnlyList<LaunchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByPeriod([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (from > to)
            return BadRequest(new { message = "'from' must be less than or equal to 'to'." });

        var result = await launchService.GetByPeriodAsync(from, to, ct);
        return Ok(result);
    }
}
