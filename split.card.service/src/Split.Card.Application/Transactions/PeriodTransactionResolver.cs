using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.Transactions;

/// <summary>
/// A Transaction resolved against a specific period: PeriodAmount is what actually applies
/// THIS period (the full Amount for a one-time purchase, or InstallmentPlan.InstallmentAmount
/// for an installment purchase) — never Transaction.Amount directly once an InstallmentPlan
/// is involved. InstallmentNumber/TotalInstallments are null for non-installment purchases.
/// </summary>
public sealed record ResolvedPeriodTransaction(
    Transaction Transaction,
    decimal PeriodAmount,
    int? InstallmentNumber,
    int? TotalInstallments);

/// <summary>
/// Figures out which of a card's transactions actually belong to a given statement period,
/// and at what amount. This exists because of a real bug: querying transactions by
/// "PurchaseDate falls within this period's start/end" (ITransactionRepository.
/// GetByCardAndDateRangeAsync) only ever matches an installment transaction's FIRST period —
/// its PurchaseDate never changes, so months 2, 3, ... of a 3-month installment plan were
/// silently showing nothing, while month 1 was showing the FULL purchase amount instead of
/// just that month's installment. Every handler that needs "what's owed on this card this
/// period" (GetTransactionsForCardPeriodQuery, GenerateStatementPdfQuery,
/// GetHouseholdDashboardQuery) must go through this, not query the repository directly by
/// date range.
/// </summary>
public static class PeriodTransactionResolver
{
    public static async Task<IReadOnlyList<ResolvedPeriodTransaction>> Resolve(
        string cardId,
        DateOnly periodStart,
        DateOnly periodEnd,
        ITransactionRepository transactionRepository,
        IInstallmentPlanRepository installmentPlanRepository,
        CancellationToken cancellationToken)
    {
        var allTransactions = await transactionRepository.GetByCardIdAsync(cardId, cancellationToken);
        var resolved = new List<ResolvedPeriodTransaction>();

        foreach (var transaction in allTransactions)
        {
            if (transaction.InstallmentPlanId is null)
            {
                if (transaction.PurchaseDate >= periodStart && transaction.PurchaseDate <= periodEnd)
                {
                    resolved.Add(new ResolvedPeriodTransaction(transaction, transaction.Amount, null, null));
                }

                continue;
            }

            var plan = await installmentPlanRepository.GetByIdAsync(transaction.InstallmentPlanId, cancellationToken);

            if (plan is null || !plan.HasActiveInstallmentIn(periodStart, periodEnd))
            {
                continue;
            }

            var installmentNumber = plan.GetInstallmentNumberFor(periodEnd);
            var periodAmount = transaction.GetPeriodAmount(plan);

            resolved.Add(new ResolvedPeriodTransaction(transaction, periodAmount, installmentNumber, plan.TotalInstallments));
        }

        return resolved;
    }
}
