using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Common;
using SplitCard.Application.Transactions;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Application.Tests;

/// <summary>
/// Regression coverage for the cross-household leak: before HouseholdAccessGuard existed,
/// an Owner of household-1 passing household-2's id got isOwner=true (their own role,
/// never checked against which household they belong to) and received ALL of household-2's
/// transactions. These tests exist specifically to make that impossible to reintroduce
/// silently.
/// </summary>
public class GetVisibleTransactionsQueryHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    [Fact]
    public async Task Handle_OwnerQueryingOwnHousehold_ReturnsTransactions()
    {
        var users = new InMemoryUserRepository();
        var transactions = new InMemoryTransactionRepository();
        users.Seed(OwnerUser("user-1", "household-1"));

        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 100)]));

        var handler = new GetVisibleTransactionsQueryHandler(users, transactions);

        var result = await handler.Handle(new GetVisibleTransactionsQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_OwnerQueryingDifferentHousehold_ThrowsForbidden_DoesNotLeakOtherHouseholdData()
    {
        var users = new InMemoryUserRepository();
        var transactions = new InMemoryTransactionRepository();

        // user-1 is Owner of household-1, but tries to read household-2's transactions.
        users.Seed(OwnerUser("user-1", "household-1"));

        transactions.Transactions.Add(new Transaction(
            "tx-household-2", "card-in-household-2", "Comercio Ajeno", new DateOnly(2026, 3, 10), 999999, Currency.CRC, null, "user-in-household-2",
            [new PersonShare("user-in-household-2", 100)]));

        var handler = new GetVisibleTransactionsQueryHandler(users, transactions);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetVisibleTransactionsQuery("household-2", "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownActingUser_ThrowsNotFound()
    {
        var users = new InMemoryUserRepository();
        var transactions = new InMemoryTransactionRepository();

        var handler = new GetVisibleTransactionsQueryHandler(users, transactions);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetVisibleTransactionsQuery("household-1", "missing-user"), CancellationToken.None));
    }
}
