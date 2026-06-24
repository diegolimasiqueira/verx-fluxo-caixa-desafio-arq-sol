using CashFlow.DailyBalanceWorker.Data;
using CashFlow.DailyBalanceWorker.Domain;
using CashFlow.LaunchService.Api.Domain.Events;
using CashFlow.Observability;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.DailyBalanceWorker.Consumers;

public class LaunchRegisteredConsumer(WorkerDbContext db, ILogger<LaunchRegisteredConsumer> logger)
    : IConsumer<LaunchRegisteredEvent>
{
    public async Task Consume(ConsumeContext<LaunchRegisteredEvent> context)
    {
        try
        {
            var evt = context.Message;

            logger.LogInformation("[business] Processing LaunchRegistered event. LaunchId={LaunchId} Date={Date} Type={Type} Amount={Amount}",
                evt.LaunchId, evt.Date, evt.Type, evt.Amount);

            var balance = await db.DailyBalances.FirstOrDefaultAsync(b => b.Date == evt.Date, context.CancellationToken);

            if (balance is null)
            {
                balance = new DailyBalance { Date = evt.Date };
                db.DailyBalances.Add(balance);
                logger.LogDebug("[application] Creating new DailyBalance record for Date={Date}", evt.Date);
            }

            if (evt.Type.Equals("credit", StringComparison.OrdinalIgnoreCase))
                balance.TotalCredits += evt.Amount;
            else
                balance.TotalDebits += evt.Amount;

            balance.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(context.CancellationToken);

            CashFlowMeters.WorkerConsolidations.Add(1);

            logger.LogInformation("[business] DailyBalance updated for Date={Date}. Credits={Credits} Debits={Debits} Consolidated={Consolidated}",
                balance.Date, balance.TotalCredits, balance.TotalDebits, balance.ConsolidatedBalance);
        }
        catch (Exception ex)
        {
            CashFlowMeters.WorkerConsolidationErrors.Add(1);
            logger.LogError(ex, "[application] Failed to consolidate LaunchRegistered event");
            throw;
        }
    }
}
