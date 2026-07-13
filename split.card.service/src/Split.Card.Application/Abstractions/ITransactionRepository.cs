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
    /// ALL transactions for a card, regardless of PurchaseDate — needed because an
    /// installment transaction's PurchaseDate only ever falls in its FIRST period, but the
    /// transaction still "applies" (for a different, smaller amount) to every subsequent
    /// period through InstallmentPlan.TotalInstallments. Use with
    /// SplitCard.Application.Transactions.PeriodTransactionResolver to figure out which of
    /// these actually belong to a given period — don't just filter by PurchaseDate again,
    /// that's the bug this method exists to let you avoid.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetByCardIdAsync(string cardId, CancellationToken cancellationToken);

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
