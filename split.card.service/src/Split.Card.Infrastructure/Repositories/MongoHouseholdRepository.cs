using MongoDB.Driver;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence;
using SplitCard.Infrastructure.Persistence.Mappings;

namespace SplitCard.Infrastructure.Repositories;

public sealed class MongoHouseholdRepository(MongoDbContext context) : IHouseholdRepository
{
    public async Task<Household?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var document = await context.Households.Find(h => h.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task AddAsync(Household household, CancellationToken cancellationToken)
    {
        await context.Households.InsertOneAsync(household.ToDocument(), cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Household household, CancellationToken cancellationToken)
    {
        await context.Households.ReplaceOneAsync(h => h.Id == household.Id, household.ToDocument(), cancellationToken: cancellationToken);
    }
}
