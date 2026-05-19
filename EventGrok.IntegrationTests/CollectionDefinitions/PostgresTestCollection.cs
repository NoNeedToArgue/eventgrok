using EventGrok.IntegrationTests.Fixtures;

namespace EventGrok.IntegrationTests.CollectionDefinitions;

[CollectionDefinition(nameof(PostgresTestCollection))]
public class PostgresTestCollection : ICollectionFixture<PostgresContainerFixture> {}