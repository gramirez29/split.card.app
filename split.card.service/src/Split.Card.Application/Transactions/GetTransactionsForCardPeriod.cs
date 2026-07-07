using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.Transactions;

public sealed record GetTransactionsForCardPeriodQuery(string CardId, DateOnly Date, string ActingUserId);

/// <summary>
/// Backs the mobile "card detail" screen (currently a placeholder in split.card.mobile).
/// Resolves the StatementPeriod containing Date, then applies the same Split[].PersonId
/// visibility rule as GetVisibleTransactionsQuery — the Owner sees the full period,
/// everyone else only their own share.
/// </summary>
public sealed class GetTransactionsForCardPeriodQueryHandler(
    IUserRepository userRepository,
    IStatementPeriodRepository statementPeriodRepository,
    ITransactionRepository transactionRepository)
{
    public async Task<IReadOnlyList<Transaction>> Handle(GetTransactionsForCardPeriodQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        var period = await statementPeriodRepository.GetByCardAndDateAsync(query.CardId, query.Date, cancellationToken)
            ?? throw new NotFoundException($"No statement period found for card {query.CardId} on {query.Date}.");

        var transactions = await transactionRepository.GetByCardAndDateRangeAsync(
            query.CardId,
            period.StartDate,
            period.EndDate,
            cancellationToken);

        return actingUser.CanReadAll()
            ? transactions
            : transactions.Where(t => t.IsVisibleTo(query.ActingUserId)).ToList();
    }
}
