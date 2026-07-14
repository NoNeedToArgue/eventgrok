using EventGrok.Events.IntegrationTests.Fixtures;

namespace EventGrok.Events.IntegrationTests.CollectionDefinitions;

[CollectionDefinition(nameof(PostgresTestCollection))]
public class PostgresTestCollection : ICollectionFixture<PostgresContainerFixture> {}