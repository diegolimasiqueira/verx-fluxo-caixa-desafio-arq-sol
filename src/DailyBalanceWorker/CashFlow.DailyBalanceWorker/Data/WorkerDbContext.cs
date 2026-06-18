using CashFlow.DailyBalanceWorker.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.DailyBalanceWorker.Data;

public class WorkerDbContext(DbContextOptions<WorkerDbContext> options) : DbContext(options)
{
    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DailyBalance>(entity =>
        {
            entity.HasKey(e => e.Date);
            entity.Property(e => e.TotalCredits).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.TotalDebits).HasPrecision(18, 2).IsRequired();
            entity.Ignore(e => e.ConsolidatedBalance);
            entity.Property(e => e.UpdatedAt).IsRequired();
        });
    }
}
