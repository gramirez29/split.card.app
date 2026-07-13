using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Alerts;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Application.Tests;

public class GetHouseholdAlertsQueryHandlerTests
{
    private static readonly DateOnly Today = new(2026, 3, 10);

    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    private static (
        GetHouseholdAlertsQueryHandler Handler,
        InMemoryUserRepository Users,
        InMemoryCardRepository Cards,
        InMemoryTransactionRepository Transactions,
        InMemoryInstallmentPlanRepository Plans) BuildHandler()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        var transactions = new InMemoryTransactionRepository();
        var plans = new InMemoryInstallmentPlanRepository();
        var clock = new FakeClock(Today);

        var handler = new GetHouseholdAlertsQueryHandler(users, cards, transactions, plans, clock);

        return (handler, users, cards, transactions, plans);
    }

    [Fact]
    public async Task Handle_CutoffWithinWindow_ReturnsCutoffSoonAlert()
    {
        var (handler, users, cards, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        // cutoffDay=12 -> 2 días desde Today(10), dentro de la ventana de 5 días.
        // paymentDueDay=25 -> lejos, no dispara PaymentDueSoon.
        cards.Seed(new SplitCard.Domain.Entities.Card("card-1", "household-1", "Test Card", "Test Bank", CardType.Credit, 12, 25, "user-1"));

        var result = await handler.Handle(new GetHouseholdAlertsQuery("household-1", "user-1"), CancellationToken.None);

        var alert = Assert.Single(result.CardAlerts);
        Assert.Equal("CutoffSoon", alert.AlertType);
        Assert.Equal(2, alert.DaysUntil);
        Assert.Empty(result.InstallmentAlerts);
    }

    [Fact]
    public async Task Handle_PaymentDueWithinWindow_ReturnsPaymentDueSoonAlertAndActiveInstallment()
    {
        var (handler, users, cards, transactions, plans) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        // cutoffDay=12 (2 días), paymentDueDay=14 (4 días) -> ambos dentro de la ventana.
        cards.Seed(new SplitCard.Domain.Entities.Card("card-1", "household-1", "Test Card", "Test Bank", CardType.Credit, 12, 14, "user-1"));

        // Periodo actual: 2026-02-13 a 2026-03-12. Compra original en el periodo anterior
        // (FirstChargeDate 2026-02-20), así que en el periodo actual es la cuota 2 de 3.
        var plan = InstallmentPlan.CreateForNewPurchase("plan-1", 3, 4000m, Currency.CRC, new DateOnly(2026, 2, 20));
        plans.Plans.Add(plan);
        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "PriceSmart", new DateOnly(2026, 2, 20), 12000m, Currency.CRC, "plan-1", "user-1",
            [new PersonShare("user-1", 100)]));

        var result = await handler.Handle(new GetHouseholdAlertsQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Contains(result.CardAlerts, a => a.AlertType == "PaymentDueSoon" && a.DaysUntil == 4);

        var installmentAlert = Assert.Single(result.InstallmentAlerts);
        Assert.Equal("PriceSmart", installmentAlert.Merchant);
        Assert.Equal(4000m, installmentAlert.PeriodAmount);
        Assert.Equal(2, installmentAlert.InstallmentNumber);
        Assert.Equal(3, installmentAlert.TotalInstallments);
        Assert.Equal(4, installmentAlert.DaysUntilPaymentDue);
    }

    [Fact]
    public async Task Handle_DatesOutsideWindow_ReturnsNoAlertsForThatCard()
    {
        var (handler, users, cards, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        // cutoffDay=25 (15 días), paymentDueDay=30 (20 días) -> ambos fuera de ventana.
        cards.Seed(new SplitCard.Domain.Entities.Card("card-1", "household-1", "Test Card", "Test Bank", CardType.Credit, 25, 30, "user-1"));

        var result = await handler.Handle(new GetHouseholdAlertsQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Empty(result.CardAlerts);
        Assert.Empty(result.InstallmentAlerts);
    }

    [Fact]
    public async Task Handle_DebitCardsAreExcluded()
    {
        var (handler, users, cards, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(new SplitCard.Domain.Entities.Card("debit-1", "household-1", "Debit Card", "Test Bank", CardType.Debit, null, null, "user-1"));

        var result = await handler.Handle(new GetHouseholdAlertsQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Empty(result.CardAlerts);
    }

    [Fact]
    public async Task Handle_DifferentHousehold_ThrowsForbidden()
    {
        var (handler, users, cards, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(new SplitCard.Domain.Entities.Card("card-1", "household-2", "Test Card", "Test Bank", CardType.Credit, 12, 14, "user-1"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetHouseholdAlertsQuery("household-2", "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownActingUser_ThrowsNotFound()
    {
        var (handler, _, _, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetHouseholdAlertsQuery("household-1", "missing-user"), CancellationToken.None));
    }
}
