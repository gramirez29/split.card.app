using MongoDB.Driver;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence;
using SplitCard.Infrastructure.Persistence.Mappings;

namespace SplitCard.Infrastructure.Repositories;

public sealed class MongoCardRepository(MongoDbContext context) : ICardRepository
{
    public async Task<Card?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var document = await context.Cards.Find(c => c.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<Card>> GetByHouseholdIdAsync(string householdId, CancellationToken cancellationToken)
    {
        var documents = await context.Cards.Find(c => c.HouseholdId == householdId).ToListAsync(cancellationToken);
        return documents.Select(d => d.ToDomain()).ToList();
    }

    public async Task AddAsync(Card card, CancellationToken cancellationToken)
    {
        await context.Cards.InsertOneAsync(card.ToDocument(), cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Card card, CancellationToken cancellationToken)
    {
        await context.Cards.ReplaceOneAsync(c => c.Id == card.Id, card.ToDocument(), cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await context.Cards.DeleteOneAsync(c => c.Id == id, cancellationToken);
    }
}
