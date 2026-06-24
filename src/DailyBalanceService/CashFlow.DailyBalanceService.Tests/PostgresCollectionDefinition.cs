using CashFlow.Testing.Common;

namespace CashFlow.DailyBalanceService.Tests;

[CollectionDefinition(PostgresCollection.Name)]
public class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>;
