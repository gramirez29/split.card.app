using SplitCard.Domain.Entities;

namespace SplitCard.Application.Abstractions;

public interface IStatementPeriodRepository
{
    Task<StatementPeriod?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<StatementPeriod?> GetByCardAndDateAsync(string cardId, DateOnly date, CancellationToken cancellationToken);
    Task AddAsync(StatementPeriod statementPeriod, CancellationToken cancellationToken);
    Task UpdateAsync(StatementPeriod statementPeriod, CancellationToken cancellationToken);
}
