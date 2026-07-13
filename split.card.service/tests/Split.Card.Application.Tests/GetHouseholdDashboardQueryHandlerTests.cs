using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Common;
using SplitCard.Application.Reporting;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Application.Tests;

public class GetHouseholdDashboardQueryHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    private static User ContributorUser(string id, string householdId) =>
        new(id, householdId, "Contributor", $"{id}@test.com", "hash", UserRole.Contributor);

    private static SplitCard.Domain.Entities.Card CreditCard(string id, string householdId) =>
        new(id, householdId, "Test Card", "Test Bank", CardType.Credit, 20, 5, "owner-id");

    private static (
        GetHouseholdDashboardQueryHandler Handler,
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

        var handler = new GetHouseholdDashboardQueryHandler(users, cards, periods, transactions, plans);

        return (handler, users, cards, periods, transactions, plans);
    }

    [Fact]
    public async Task Handle_OwnerWithTransactions_AggregatesByCardAndByPerson()
    {
        var (handler, users, cards, periods, transactions, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        users.Seed(new User("user-2", "household-1", "María", "maria@test.com", "hash", UserRole.Contributor));
        cards.Seed(CreditCard("card-1", "household-1"));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", today.AddDays(-10), today.AddDays(10), today.AddDays(25)));

        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "Automercado", today, 10000, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 50), new PersonShare("user-2", 50)]));
        transactions.Transactions.Add(new Transaction(
            "tx-2", "card-1", "PriceSmart", today, 100, Currency.USD, null, "user-1",
            [new PersonShare("user-1", 100)]));

        var result = await handler.Handle(new GetHouseholdDashboardQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Equal(10000, result.GrandTotalCrc);
        Assert.Equal(100, result.GrandTotalUsd);

        var cardTotal = Assert.Single(result.ByCard);
        Assert.Equal(10000, cardTotal.TotalCrc);
        Assert.Equal(100, cardTotal.TotalUsd);

        var ownerTotal = result.ByPerson.Single(p => p.PersonId == "user-1");
        Assert.Equal(5000, ownerTotal.TotalCrc);
        Assert.Equal(100, ownerTotal.TotalUsd);

        var mariaTotal = result.ByPerson.Single(p => p.PersonId == "user-2");
        Assert.Equal("María", mariaTotal.PersonName);
        Assert.Equal(5000, mariaTotal.TotalCrc);
        Assert.Equal(0, mariaTotal.TotalUsd);
    }

    [Fact]
    public async Task Handle_InstallmentPurchase_UsesInstallmentAmountNotFullTotal()
    {
        // Este es el bug reportado: una compra de ₡12,000 a 3 cuotas debía sumar ₡4,000
        // al total del periodo actual, no ₡12,000.
        var (handler, users, cards, periods, transactions, plans) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", today.AddDays(-10), today.AddDays(10), today.AddDays(25)));

        var plan = InstallmentPlan.CreateForNewPurchase("plan-1", 3, 4000m, Currency.CRC, today);
        plans.Plans.Add(plan);
        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "PriceSmart", today, 12000m, Currency.CRC, "plan-1", "user-1",
            [new PersonShare("user-1", 100)]));

        var result = await handler.Handle(new GetHouseholdDashboardQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Equal(4000m, result.GrandTotalCrc);

        var cardTotal = Assert.Single(result.ByCard);
        Assert.Equal(4000m, cardTotal.TotalCrc);

        var ownerTotal = Assert.Single(result.ByPerson);
        Assert.Equal(4000m, ownerTotal.TotalCrc);
    }

    [Fact]
    public async Task Handle_ContributorNotInSplit_DoesNotSeeThatTransactionInTotals()
    {
        var (handler, users, cards, periods, transactions, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        users.Seed(ContributorUser("user-2", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", today.AddDays(-10), today.AddDays(10), today.AddDays(25)));

        // Solo el Owner aparece en el split — user-2 no debería ver nada de esto.
        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "Compra personal del Owner", today, 5000, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 100)]));

        var result = await handler.Handle(new GetHouseholdDashboardQuery("household-1", "user-2"), CancellationToken.None);

        Assert.Equal(0, result.GrandTotalCrc);
        Assert.Empty(result.ByPerson);
    }

    [Fact]
    public async Task Handle_CreditCardWithNoStatementPeriodYet_ReturnsZeroForThatCard()
    {
        var (handler, users, cards, _, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));

        var result = await handler.Handle(new GetHouseholdDashboardQuery("household-1", "user-1"), CancellationToken.None);

        var cardTotal = Assert.Single(result.ByCard);
        Assert.Equal(0, cardTotal.TotalCrc);
        Assert.Equal(0, cardTotal.TotalUsd);
    }

    [Fact]
    public async Task Handle_DebitCardsAreExcluded()
    {
        var (handler, users, cards, _, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(new SplitCard.Domain.Entities.Card("debit-1", "household-1", "Debit Card", "Test Bank", CardType.Debit, null, null, "user-1"));

        var result = await handler.Handle(new GetHouseholdDashboardQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Empty(result.ByCard);
    }

    [Fact]
    public async Task Handle_DifferentHousehold_ThrowsForbidden()
    {
        var (handler, users, cards, _, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-2"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetHouseholdDashboardQuery("household-2", "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownActingUser_ThrowsNotFound()
    {
        var (handler, _, _, _, _, _) = BuildHandler();

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetHouseholdDashboardQuery("household-1", "missing-user"), CancellationToken.None));
    }
}
