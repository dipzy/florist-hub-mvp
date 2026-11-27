namespace FloristHub.Api.IntegrationTests;

[CollectionDefinition(PostgresApiCollection.Name)]
public sealed class PostgresApiCollection : ICollectionFixture<FloristHubPostgresFixture>
{
    public const string Name = "postgres-api";
}
