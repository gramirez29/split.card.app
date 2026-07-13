using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Application.Transactions;
using SplitCard.Domain.Enums;

namespace SplitCard.Application.Reporting;

public sealed record GetHouseholdDashboardQuery(string HouseholdId, string ActingUserId);

public sealed record PersonTotal(string PersonId, string PersonName, decimal TotalCrc, decimal TotalUsd);

public sealed record CardTotal(string CardId, string CardName, decimal TotalCrc, decimal TotalUsd);

public sealed record HouseholdDashboard(
    decimal GrandTotalCrc,
    decimal GrandTotalUsd,
    IReadOnlyList<PersonTotal> ByPerson,
    IReadOnlyList<CardTotal> ByCard);

/// <summary>
/// Consolidated "how much is owed right now" view — product feature 5 in SplitCard.md.
/// Scope deliberately limited to Credit cards' CURRENT open StatementPeriod: Debit
/// purchases are already-settled cash, not a pending balance. A Credit card with no
/// purchases yet this period contributes zero rather than being omitted.
/// Uses PeriodTransactionResolver (not a raw date-range query) so an installment purchase
/// contributes its PER-PERIOD installment amount to every period it's active in, not the
/// full purchase total dumped into whichever period it happened to be bought in.
/// Visibility: same rule as everywhere else — Owner sees every transaction in the period,
/// everyone else only the ones where they appear in Split[]. ByPerson is built only from
/// transactions the caller can already see.
/// </summary>
public sealed class GetHouseholdDashboardQueryHandler(
    IUserRepository userRepository,
    ICardRepository cardRepository,
    IStatementPeriodRepository statementPeriodRepository,
    ITransactionRepository transactionRepository,
    IInstallmentPlanRepository installmentPlanRepository)
{
    public async Task<HouseholdDashboard> Handle(GetHouseholdDashboardQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, query.HouseholdId);

        var cards = await cardRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
        var members = await userRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
        var memberNamesById = members.ToDictionary(m => m.Id, m => m.Name);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var cardTotals = new List<CardTotal>();
        var personTotals = new Dictionary<string, (decimal Crc, decimal Usd)>();
        var grandCrc = 0m;
        var grandUsd = 0m;

        foreach (var card in cards.Where(c => c.Type == CardType.Credit))
        {
            var period = await statementPeriodRepository.GetByCardAndDateAsync(card.Id, today, cancellationToken);

            if (period is null)
            {
                cardTotals.Add(new CardTotal(card.Id, card.Name, 0m, 0m));
                continue;
            }

            var resolved = await PeriodTransactionResolver.Resolve(
                card.Id,
                period.StartDate,
                period.EndDate,
                transactionRepository,
                installmentPlanRepository,
                cancellationToken);

            var visibleTransactions = actingUser.CanReadAll()
                ? resolved
                : resolved.Where(r => r.Transaction.IsVisibleTo(query.ActingUserId)).ToList();

            var cardCrc = 0m;
            var cardUsd = 0m;

            foreach (var item in visibleTransactions)
            {
                var transaction = item.Transaction;

                if (transaction.Currency == Currency.CRC)
                {
                    cardCrc += item.PeriodAmount;
                }
                else
                {
                    cardUsd += item.PeriodAmount;
                }

                foreach (var share in transaction.Split)
                {
                    var current = personTotals.GetValueOrDefault(share.PersonId, (Crc: 0m, Usd: 0m));
                    var shareAmount = transaction.GetShareAmount(share.PersonId, item.PeriodAmount);

                    personTotals[share.PersonId] = transaction.Currency == Currency.CRC
                        ? (current.Crc + shareAmount, current.Usd)
                        : (current.Crc, current.Usd + shareAmount);
                }
            }

            cardTotals.Add(new CardTotal(card.Id, card.Name, cardCrc, cardUsd));
            grandCrc += cardCrc;
            grandUsd += cardUsd;
        }

        var personTotalsList = personTotals
            .Select(kvp => new PersonTotal(
                kvp.Key,
                memberNamesById.GetValueOrDefault(kvp.Key, kvp.Key),
                kvp.Value.Crc,
                kvp.Value.Usd))
            .OrderByDescending(p => p.TotalCrc)
            .ThenByDescending(p => p.TotalUsd)
            .ToList();

        return new HouseholdDashboard(grandCrc, grandUsd, personTotalsList, cardTotals);
    }
}
