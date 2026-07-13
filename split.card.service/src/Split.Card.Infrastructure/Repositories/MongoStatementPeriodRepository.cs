using MongoDB.Driver;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence;
using SplitCard.Infrastructure.Persistence.Mappings;

namespace SplitCard.Infrastructure.Repositories;

public sealed class MongoStatementPeriodRepository(MongoDbContext context) : IStatementPeriodRepository
{
    public async Task<StatementPeriod?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var document = await context.StatementPeriods.Find(p => p.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<StatementPeriod?> GetByCardAndDateAsync(string cardId, DateOnly date, CancellationToken cancellationToken)
    {
        var document = await context.StatementPeriods
            .Find(p => p.CardId == cardId && date >= p.StartDate && date <= p.EndDate)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task AddAsync(StatementPeriod statementPeriod, CancellationToken cancellationToken)
    {
        await context.StatementPeriods.InsertOneAsync(statementPeriod.ToDocument(), cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(StatementPeriod statementPeriod, CancellationToken cancellationToken)
    {
        await context.StatementPeriods.ReplaceOneAsync(p => p.Id == statementPeriod.Id, statementPeriod.ToDocument(), cancellationToken: cancellationToken);
    }
}
