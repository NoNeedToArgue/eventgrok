using EventGrok.Bookings.IntegrationTests.Fixtures;

namespace EventGrok.Bookings.IntegrationTests.CollectionDefinitions;

[CollectionDefinition(nameof(PostgresTestCollection))]
public class PostgresTestCollection : ICollectionFixture<PostgresContainerFixture> {}