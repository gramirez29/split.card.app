using MongoDB.Driver;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence;
using SplitCard.Infrastructure.Persistence.Documents;
using SplitCard.Infrastructure.Persistence.Mappings;

namespace SplitCard.Infrastructure.Repositories;

public sealed class MongoTransactionRepository(MongoDbContext context) : ITransactionRepository
{
    public async Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var document = await context.Transactions.Find(t => t.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<Transaction>> GetByCardAndDateRangeAsync(
        string cardId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var documents = await context.Transactions
            .Find(t => t.CardId == cardId && t.PurchaseDate >= startDate && t.PurchaseDate <= endDate)
            .ToListAsync(cancellationToken);

        return documents.Select(d => d.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<Transaction>> GetVisibleToUserAsync(
        string householdId,
        string userId,
        bool isOwner,
        CancellationToken cancellationToken)
    {
        var cardIds = await context.Cards
            .Find(c => c.HouseholdId == householdId)
            .Project(c => c.Id)
            .ToListAsync(cancellationToken);

        var filterBuilder = Builders<TransactionDocument>.Filter;
        var filter = filterBuilder.In(t => t.CardId, cardIds);

        if (!isOwner)
        {
            filter &= filterBuilder.ElemMatch(t => t.Split, s => s.PersonId == userId);
        }

        var documents = await context.Transactions.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(d => d.ToDomain()).ToList();
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        await context.Transactions.InsertOneAsync(transaction.ToDocument(), cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        await context.Transactions.ReplaceOneAsync(t => t.Id == transaction.Id, transaction.ToDocument(), cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await context.Transactions.DeleteOneAsync(t => t.Id == id, cancellationToken);
    }
}
