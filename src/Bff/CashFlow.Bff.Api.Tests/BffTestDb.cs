using CashFlow.Bff.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Bff.Api.Tests;

internal static class BffTestDb
{
    public static BffDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<BffDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new BffDbContext(options);
    }
}
