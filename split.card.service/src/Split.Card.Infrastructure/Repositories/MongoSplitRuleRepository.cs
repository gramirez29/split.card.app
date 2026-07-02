using MongoDB.Driver;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence;
using SplitCard.Infrastructure.Persistence.Mappings;

namespace SplitCard.Infrastructure.Repositories;

public sealed class MongoSplitRuleRepository(MongoDbContext context) : ISplitRuleRepository
{
    public async Task<IReadOnlyList<SplitRule>> GetByHouseholdIdAsync(string householdId, CancellationToken cancellationToken)
    {
        var documents = await context.SplitRules.Find(r => r.HouseholdId == householdId).ToListAsync(cancellationToken);
        return documents.Select(d => d.ToDomain()).ToList();
    }

    public async Task AddAsync(SplitRule splitRule, CancellationToken cancellationToken)
    {
        await context.SplitRules.InsertOneAsync(splitRule.ToDocument(), cancellationToken: cancellationToken);
    }
}
