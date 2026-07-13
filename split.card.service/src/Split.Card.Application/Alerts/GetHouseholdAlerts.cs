using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Application.Transactions;
using SplitCard.Domain.Enums;

namespace SplitCard.Application.Alerts;

public sealed record GetHouseholdAlertsQuery(string HouseholdId, string ActingUserId);

public sealed record CardAlert(
    string CardId,
    string CardName,
    string AlertType, // "CutoffSoon" | "PaymentDueSoon"
    DateOnly Date,
    int DaysUntil);

public sealed record InstallmentAlert(
    string CardId,
    string CardName,
    string TransactionId,
    string Merchant,
    decimal PeriodAmount,
    Currency Currency,
    int InstallmentNumber,
    int TotalInstallments,
    DateOnly PaymentDueDate,
    int DaysUntilPaymentDue);

public sealed record HouseholdAlerts(
    IReadOnlyList<CardAlert> CardAlerts,
    IReadOnlyList<InstallmentAlert> InstallmentAlerts);

/// <summary>
/// In-app-only version of product features 3 and 4 in SplitCard.md ("alertas de cuotas
/// por vencer" / "notificación de corte/pago próximo") — no push notifications, just data
/// the mobile Home tab renders when the app is opened. Scope: Credit cards only (Debit has
/// no cutoff/due date concept at all). Uses Card.GetStatementPeriodFor(today) directly —
/// NOT the persisted StatementPeriod repository — so an alert fires even if no purchase
/// has been registered yet this period (a StatementPeriod document might not exist yet,
/// but the cutoff/due date still approaches regardless).
///
/// CardAlerts fire for both the cutoff and the payment due date, independently, whenever
/// either is within ALERT_WINDOW_DAYS. InstallmentAlerts (which specific installment
/// purchases are part of the upcoming payment) are only computed when PaymentDueSoon
/// fires — tied to the moment money actually needs to be paid, not the cutoff itself.
/// </summary>
public sealed class GetHouseholdAlertsQueryHandler(
    IUserRepository userRepository,
    ICardRepository cardRepository,
    ITransactionRepository transactionRepository,
    IInstallmentPlanRepository installmentPlanRepository,
    IClock clock)
{
    private const int AlertWindowDays = 5;

    public async Task<HouseholdAlerts> Handle(GetHouseholdAlertsQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, query.HouseholdId);

        var cards = await cardRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
        var today = clock.Today;

        var cardAlerts = new List<CardAlert>();
        var installmentAlerts = new List<InstallmentAlert>();

        foreach (var card in cards.Where(c => c.Type == CardType.Credit))
        {
            var period = card.GetStatementPeriodFor(today);

            var daysUntilCutoff = period.EndDate.DayNumber - today.DayNumber;
            if (daysUntilCutoff <= AlertWindowDays)
            {
                cardAlerts.Add(new CardAlert(card.Id, card.Name, "CutoffSoon", period.EndDate, daysUntilCutoff));
            }

            var daysUntilPaymentDue = period.PaymentDueDate.DayNumber - today.DayNumber;
            if (daysUntilPaymentDue <= AlertWindowDays)
            {
                cardAlerts.Add(new CardAlert(card.Id, card.Name, "PaymentDueSoon", period.PaymentDueDate, daysUntilPaymentDue));

                var resolved = await PeriodTransactionResolver.Resolve(
                    card.Id,
                    period.StartDate,
                    period.EndDate,
                    transactionRepository,
                    installmentPlanRepository,
                    cancellationToken);

                var visible = actingUser.CanReadAll()
                    ? resolved
                    : resolved.Where(r => r.Transaction.IsVisibleTo(query.ActingUserId)).ToList();

                foreach (var item in visible.Where(r => r.InstallmentNumber is not null && r.TotalInstallments is not null))
                {
                    installmentAlerts.Add(new InstallmentAlert(
                        card.Id,
                        card.Name,
                        item.Transaction.Id,
                        item.Transaction.Merchant,
                        item.PeriodAmount,
                        item.Transaction.Currency,
                        item.InstallmentNumber!.Value,
                        item.TotalInstallments!.Value,
                        period.PaymentDueDate,
                        daysUntilPaymentDue));
                }
            }
        }

        return new HouseholdAlerts(cardAlerts, installmentAlerts);
    }
}
