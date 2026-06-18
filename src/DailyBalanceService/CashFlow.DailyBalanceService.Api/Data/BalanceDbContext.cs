using CashFlow.DailyBalanceService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.DailyBalanceService.Api.Data;

public class BalanceDbContext(DbContextOptions<BalanceDbContext> options) : DbContext(options)
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
