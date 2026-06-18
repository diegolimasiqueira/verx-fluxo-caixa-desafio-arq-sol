using CashFlow.DailyBalanceService.Api.Data;
using CashFlow.DailyBalanceService.Api.DTOs;
using CashFlow.DailyBalanceService.Api.Middleware;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.DailyBalanceService.Api.Services;

public class DailyBalanceQueryService(BalanceDbContext db, ILogger<DailyBalanceQueryService> logger)
{
    public async Task<DailyBalanceResponse> GetByDateAsync(DateOnly date, CancellationToken ct)
    {
        logger.LogDebug("[application] Querying consolidated balance for Date={Date}", date);

        var balance = await db.DailyBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Date == date, ct);

        if (balance is null)
        {
            logger.LogInformation("[business] No consolidated balance found for Date={Date}", date);
            throw new NotFoundException($"No consolidated balance found for date {date:yyyy-MM-dd}.");
        }

        logger.LogInformation("[business] Balance retrieved for Date={Date} Consolidated={Balance}",
            date, balance.ConsolidatedBalance);

        return new DailyBalanceResponse(
            balance.Date,
            balance.TotalCredits,
            balance.TotalDebits,
            balance.ConsolidatedBalance,
            balance.UpdatedAt);
    }

    public async Task<IReadOnlyList<DailyBalanceResponse>> GetByPeriodAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        logger.LogDebug("[application] Querying consolidated balances From={From} To={To}", from, to);

        var balances = await db.DailyBalances
            .AsNoTracking()
            .Where(b => b.Date >= from && b.Date <= to)
            .OrderBy(b => b.Date)
            .ToListAsync(ct);

        return balances.Select(b => new DailyBalanceResponse(
            b.Date, b.TotalCredits, b.TotalDebits, b.ConsolidatedBalance, b.UpdatedAt)).ToList();
    }
}
