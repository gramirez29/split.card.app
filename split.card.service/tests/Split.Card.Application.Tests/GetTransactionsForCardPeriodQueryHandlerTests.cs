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

    [Fact]
    public async Task Handle_SameHousehold_ReturnsTransactionsInPeriod()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        var periods = new InMemoryStatementPeriodRepository();
        var transactions = new InMemoryTransactionRepository();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", new DateOnly(2026, 2, 21), new DateOnly(2026, 3, 20), new DateOnly(2026, 4, 5)));
        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 100)]));

        var handler = new GetTransactionsForCardPeriodQueryHandler(users, cards, periods, transactions);

        var result = await handler.Handle(
            new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 3, 15), "user-1"),
            CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_CardBelongsToDifferentHousehold_ThrowsForbidden()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        var periods = new InMemoryStatementPeriodRepository();
        var transactions = new InMemoryTransactionRepository();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-2")); // different household

        var handler = new GetTransactionsForCardPeriodQueryHandler(users, cards, periods, transactions);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownCard_ThrowsNotFound()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        var periods = new InMemoryStatementPeriodRepository();
        var transactions = new InMemoryTransactionRepository();

        users.Seed(OwnerUser("user-1", "household-1"));

        var handler = new GetTransactionsForCardPeriodQueryHandler(users, cards, periods, transactions);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetTransactionsForCardPeriodQuery("missing-card", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoStatementPeriodForDate_ThrowsNotFound()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        var periods = new InMemoryStatementPeriodRepository();
        var transactions = new InMemoryTransactionRepository();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));
        // No StatementPeriod seeded for this date.

        var handler = new GetTransactionsForCardPeriodQueryHandler(users, cards, periods, transactions);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetTransactionsForCardPeriodQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None));
    }
}
