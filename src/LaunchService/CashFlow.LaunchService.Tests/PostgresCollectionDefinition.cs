using CashFlow.Testing.Common;

namespace CashFlow.LaunchService.Tests;

[CollectionDefinition(PostgresCollection.Name)]
public class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>;
