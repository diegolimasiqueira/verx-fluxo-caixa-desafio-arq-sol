using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace CashFlow.Testing.Common;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var conn = new NpgsqlConnection(_container.GetConnectionString());
        await conn.OpenAsync();

        foreach (var db in new[] { "bff_db", "launch_db", "daily_balance_db" })
        {
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{db}\"", conn);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public string GetConnectionString(string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = database
        };
        return builder.ConnectionString;
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

public static class PostgresCollection
{
    public const string Name = "Postgres";
}
