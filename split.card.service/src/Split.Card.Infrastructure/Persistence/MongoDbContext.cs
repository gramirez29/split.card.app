using MongoDB.Driver;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(MongoDbSettings settings)
    {
        var clientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);
        clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
        clientSettings.ConnectTimeout = TimeSpan.FromSeconds(10);
        clientSettings.RetryWrites = true;
        clientSettings.RetryReads = true;

        var client = new MongoClient(clientSettings);
        _database = client.GetDatabase(settings.DatabaseName);
    }

    public IMongoCollection<HouseholdDocument> Households => _database.GetCollection<HouseholdDocument>("households");
    public IMongoCollection<UserDocument> Users => _database.GetCollection<UserDocument>("users");
    public IMongoCollection<CardDocument> Cards => _database.GetCollection<CardDocument>("cards");
    public IMongoCollection<InstallmentPlanDocument> InstallmentPlans => _database.GetCollection<InstallmentPlanDocument>("installment_plans");
    public IMongoCollection<TransactionDocument> Transactions => _database.GetCollection<TransactionDocument>("transactions");
    public IMongoCollection<StatementPeriodDocument> StatementPeriods => _database.GetCollection<StatementPeriodDocument>("statement_periods");
    public IMongoCollection<SplitRuleDocument> SplitRules => _database.GetCollection<SplitRuleDocument>("split_rules");

    /// <summary>
    /// Used by the API health check to detect connection drops/timeouts against MongoDB.
    /// </summary>
    public async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>("{ping:1}", cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoException)
        {
            return false;
        }
    }
}
