using EventGrok.Users.IntegrationTests.Fixtures;

namespace EventGrok.Users.IntegrationTests.CollectionDefinitions;

[CollectionDefinition(nameof(PostgresTestCollection))]
public class PostgresTestCollection : ICollectionFixture<PostgresContainerFixture> {}