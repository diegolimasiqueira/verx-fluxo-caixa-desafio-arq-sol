using CashFlow.LaunchService.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.LaunchService.Api.Data;

public class LaunchDbContext(DbContextOptions<LaunchDbContext> options) : DbContext(options)
{
    public DbSet<Launch> Launches => Set<Launch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Launch>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
            entity.Property(e => e.Type).HasConversion<string>().IsRequired();
            entity.Property(e => e.Description).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.Date);
        });
    }
}
