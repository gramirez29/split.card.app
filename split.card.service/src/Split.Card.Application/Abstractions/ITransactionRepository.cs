using SplitCard.Domain.Entities;

namespace SplitCard.Application.Abstractions;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Transaction>> GetByCardAndDateRangeAsync(
        string cardId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies the Split[].PersonId-based visibility rule: Owner sees everything in the
    /// household; any other role only sees transactions where it appears in the split.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetVisibleToUserAsync(
        string householdId,
        string userId,
        bool isOwner,
        CancellationToken cancellationToken);

    Task AddAsync(Transaction transaction, CancellationToken cancellationToken);
    Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
