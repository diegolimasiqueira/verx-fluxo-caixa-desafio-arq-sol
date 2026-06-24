using CashFlow.Testing.Common;

namespace CashFlow.Bff.Api.Tests;

[CollectionDefinition(PostgresCollection.Name)]
public class PostgresCollectionDefinition : ICollectionFixture<PostgresFixture>;
