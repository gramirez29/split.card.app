using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Common;
using SplitCard.Application.Transactions;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Application.Tests;

public class GetTransactionsForCardPeriodQueryHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    private static SplitCard.Domain.Entities.Card CreditCard(string id, string householdId) =>
        new(id, householdId, "Test Card", "Test Bank", CardType.Credit, 20, 5, "owner-id");

    private static (
        GetTransactionsForCardPeriodQueryHandler Handler,
        InMemoryUserRepository Users,
        InMemoryCardRepository Cards,
        InMemoryStatementPeriodRepository Periods,
        InMemoryTransactionRepository Transactions,
        InMemoryInstallmentPlanRepository Plans) BuildHandler()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        var periods = new InMemoryStatementPeriodRepository();
        var transactions = new InMemoryTransactionRepository();
        var plans = new InMemoryInstallmentPlanRepository();

        var handler = new GetTransactionsForCardPeriodQueryHandler(users, cards, periods, transactions, plans);

        return (handler, users, cards, periods, transactions, plans);
    }

    [Fact]
    public async Task Handle_SameHousehold_ReturnsTransactionsInPeriod()
    {
        var (handler, users, cards, periods, transactions, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", new DateOnly(2026, 2, 21), new DateOnly(2026, 3, 20), new DateOnly(2026, 4, 5)));
        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 100)]));

        var result = await handler.Handle(
            new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 3, 15), "user-1"),
            CancellationToken.None);

        var resolved = Assert.Single(result);
        Assert.Equal(1000, resolved.PeriodAmount);
        Assert.Null(resolved.InstallmentNumber);
    }

    [Fact]
    public async Task Handle_InstallmentPurchase_ShowsInstallmentAmountNotFullTotal_AcrossThreeConsecutivePeriods()
    {
        // Escenario reportado: compra de ₡12,000 a 3 meses. Cada periodo debe mostrar
        // ₡4,000 (la cuota), no ₡12,000 (el total) — y debe aparecer en los 3 periodos
        // consecutivos, no solo en el primero.
        var (handler, users, cards, periods, transactions, plans) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));

        var plan = InstallmentPlan.CreateForNewPurchase(
            "plan-1", totalInstallments: 3, installmentAmount: 4000m, Currency.CRC, new DateOnly(2026, 3, 10));
        plans.Plans.Add(plan);

        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "PriceSmart", new DateOnly(2026, 3, 10), 12000m, Currency.CRC, "plan-1", "user-1",
            [new PersonShare("user-1", 100)]));

        periods.Periods.Add(new StatementPeriod("period-1", "card-1", new DateOnly(2026, 2, 21), new DateOnly(2026, 3, 20), new DateOnly(2026, 4, 5)));
        periods.Periods.Add(new StatementPeriod("period-2", "card-1", new DateOnly(2026, 3, 21), new DateOnly(2026, 4, 20), new DateOnly(2026, 5, 5)));
        periods.Periods.Add(new StatementPeriod("period-3", "card-1", new DateOnly(2026, 4, 21), new DateOnly(2026, 5, 20), new DateOnly(2026, 6, 5)));
        periods.Periods.Add(new StatementPeriod("period-4", "card-1", new DateOnly(2026, 5, 21), new DateOnly(2026, 6, 20), new DateOnly(2026, 7, 5)));

        var period1Result = await handler.Handle(new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None);
        var period2Result = await handler.Handle(new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 4, 15), "user-1"), CancellationToken.None);
        var period3Result = await handler.Handle(new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 5, 15), "user-1"), CancellationToken.None);
        var period4Result = await handler.Handle(new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 6, 15), "user-1"), CancellationToken.None);

        var installment1 = Assert.Single(period1Result);
        Assert.Equal(4000m, installment1.PeriodAmount);
        Assert.Equal(1, installment1.InstallmentNumber);
        Assert.Equal(3, installment1.TotalInstallments);

        var installment2 = Assert.Single(period2Result);
        Assert.Equal(4000m, installment2.PeriodAmount);
        Assert.Equal(2, installment2.InstallmentNumber);

        var installment3 = Assert.Single(period3Result);
        Assert.Equal(4000m, installment3.PeriodAmount);
        Assert.Equal(3, installment3.InstallmentNumber);

        // Ya se pagaron las 3 cuotas — el 4to periodo no debe mostrar nada de esta compra.
        Assert.Empty(period4Result);
    }

    [Fact]
    public async Task Handle_CardBelongsToDifferentHousehold_ThrowsForbidden()
    {
        var (handler, users, cards, _, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-2")); // different household

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownCard_ThrowsNotFound()
    {
        var (handler, users, _, _, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetTransactionsForCardPeriodQuery("missing-card", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoStatementPeriodForDate_ThrowsNotFound()
    {
        var (handler, users, cards, _, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));
        // No StatementPeriod seeded for this date.

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None));
    }
}
