namespace SplitCard.Infrastructure.Persistence;

public sealed class MongoDbSettings
{
    public required string ConnectionString { get; init; }
    public required string DatabaseName { get; init; }

    public static MongoDbSettings FromEnvironment()
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Environment variable MONGODB_CONNECTION_STRING is not set.");

        var databaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
            ?? throw new InvalidOperationException("Environment variable MONGODB_DATABASE_NAME is not set.");

        return new MongoDbSettings
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName
        };
    }
}
