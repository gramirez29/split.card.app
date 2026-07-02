using SplitCard.Domain.Entities;

namespace SplitCard.Application.Abstractions;

public interface ICardRepository
{
    Task<Card?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Card>> GetByHouseholdIdAsync(string householdId, CancellationToken cancellationToken);
    Task AddAsync(Card card, CancellationToken cancellationToken);
    Task UpdateAsync(Card card, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
