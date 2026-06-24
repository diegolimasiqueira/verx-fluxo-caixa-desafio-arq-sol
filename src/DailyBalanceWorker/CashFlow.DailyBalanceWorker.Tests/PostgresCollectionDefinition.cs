using CashFlow.Testing.Common;

namespace CashFlow.DailyBalanceWorker.Tests;

[CollectionDefinition(PostgresCollection.Name)]
public class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>;
