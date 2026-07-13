using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;

namespace SplitCard.Application.Transactions;

public sealed record GetTransactionsForCardPeriodQuery(string CardId, DateOnly Date, string ActingUserId);

/// <summary>
/// Backs the mobile "card detail" screen. Loads the Card to enforce HouseholdAccessGuard,
/// resolves the StatementPeriod containing Date, then uses PeriodTransactionResolver
/// (NOT a raw date-range query) so installment purchases show their per-period amount in
/// every period they're active, not just the one where they were originally purchased.
/// Applies the same Split[].PersonId visibility rule as GetVisibleTransactionsQuery.
/// </summary>
public sealed class GetTransactionsForCardPeriodQueryHandler(
    IUserRepository userRepository,
    ICardRepository cardRepository,
    IStatementPeriodRepository statementPeriodRepository,
    ITransactionRepository transactionRepository,
    IInstallmentPlanRepository installmentPlanRepository)
{
    public async Task<IReadOnlyList<ResolvedPeriodTransaction>> Handle(
        GetTransactionsForCardPeriodQuery query,
        CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        var card = await cardRepository.GetByIdAsync(query.CardId, cancellationToken)
            ?? throw new NotFoundException($"Card {query.CardId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, card.HouseholdId);

        var period = await statementPeriodRepository.GetByCardAndDateAsync(query.CardId, query.Date, cancellationToken)
            ?? throw new NotFoundException($"No statement period found for card {query.CardId} on {query.Date}.");

        var resolved = await PeriodTransactionResolver.Resolve(
            query.CardId,
            period.StartDate,
            period.EndDate,
            transactionRepository,
            installmentPlanRepository,
            cancellationToken);

        return actingUser.CanReadAll()
            ? resolved
            : resolved.Where(r => r.Transaction.IsVisibleTo(query.ActingUserId)).ToList();
    }
}
