using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Common;
using SplitCard.Application.Transactions;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Application.Tests;

public class RegisterTransactionCommandHandlerTests
{
    private static User OwnerUser(string id = "user-1", string householdId = "household-1") =>
        new(id, householdId, "Owner", "owner@test.com", "hash", UserRole.Owner);

    private static User RestrictedViewerUser(string id = "user-2", string householdId = "household-1") =>
        new(id, householdId, "Kid", "kid@test.com", "hash", UserRole.RestrictedViewer);

    private static SplitCard.Domain.Entities.Card CreditCard(string id = "card-1", string householdId = "household-1") =>
        new(id, householdId, "Test Card", "Test Bank", CardType.Credit, 20, 5, "user-1");

    private static SplitCard.Domain.Entities.Card DebitCard(string id = "card-2", string householdId = "household-1") =>
        new(id, householdId, "Debit Card", "Test Bank", CardType.Debit, null, null, "user-1");

    private static (
        RegisterTransactionCommandHandler Handler,
        InMemoryUserRepository Users,
        InMemoryCardRepository Cards,
        InMemoryInstallmentPlanRepository Plans,
        InMemoryStatementPeriodRepository Periods,
        InMemoryTransactionRepository Transactions) BuildHandler()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        var plans = new InMemoryInstallmentPlanRepository();
        var periods = new InMemoryStatementPeriodRepository();
        var transactions = new InMemoryTransactionRepository();

        var handler = new RegisterTransactionCommandHandler(users, cards, plans, periods, transactions);

        return (handler, users, cards, plans, periods, transactions);
    }

    [Fact]
    public async Task Handle_ValidPurchaseNoInstallments_CreatesTransactionAndStatementPeriod()
    {
        var (handler, users, cards, _, periods, transactions) = BuildHandler();
        users.Seed(OwnerUser());
        cards.Seed(CreditCard());

        var command = new RegisterTransactionCommand(
            ActingUserId: "user-1",
            CardId: "card-1",
            Merchant: "Automercado",
            PurchaseDate: new DateOnly(2026, 3, 10),
            Amount: 50000,
            Currency: Currency.CRC,
            Installments: null,
            Split: [new PersonShare("user-1", 100)]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Single(transactions.Transactions);
        Assert.Null(result.InstallmentPlanId);
        Assert.Single(periods.Periods);
        Assert.Equal(new DateOnly(2026, 3, 20), periods.Periods[0].EndDate);
    }

    [Fact]
    public async Task Handle_WithInstallments_CreatesInstallmentPlanLinkedToTransaction()
    {
        var (handler, users, cards, plans, _, transactions) = BuildHandler();
        users.Seed(OwnerUser());
        cards.Seed(CreditCard());

        var command = new RegisterTransactionCommand(
            ActingUserId: "user-1",
            CardId: "card-1",
            Merchant: "Tienda",
            PurchaseDate: new DateOnly(2026, 3, 10),
            Amount: 12000,
            Currency: Currency.CRC,
            Installments: new InstallmentPlanInput(TotalInstallments: 6, InstallmentAmount: 2000),
            Split: [new PersonShare("user-1", 100)]);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Single(plans.Plans);
        Assert.Equal(result.InstallmentPlanId, plans.Plans[0].Id);
        Assert.Equal(6, plans.Plans[0].TotalInstallments);
    }

    [Fact]
    public async Task Handle_SecondPurchaseSamePeriod_ReusesExistingStatementPeriod()
    {
        var (handler, users, cards, _, periods, _) = BuildHandler();
        users.Seed(OwnerUser());
        cards.Seed(CreditCard());

        var split = new List<PersonShare> { new("user-1", 100) };

        await handler.Handle(
            new RegisterTransactionCommand("user-1", "card-1", "A", new DateOnly(2026, 3, 5), 1000, Currency.CRC, null, split),
            CancellationToken.None);

        await handler.Handle(
            new RegisterTransactionCommand("user-1", "card-1", "B", new DateOnly(2026, 3, 15), 2000, Currency.CRC, null, split),
            CancellationToken.None);

        Assert.Single(periods.Periods);
    }

    [Fact]
    public async Task Handle_DebitCard_DoesNotCreateStatementPeriod()
    {
        var (handler, users, cards, _, periods, transactions) = BuildHandler();
        users.Seed(OwnerUser());
        cards.Seed(DebitCard());

        var command = new RegisterTransactionCommand(
            "user-1", "card-2", "Supermarket", new DateOnly(2026, 3, 10), 5000, Currency.CRC, null,
            [new PersonShare("user-1", 100)]);

        await handler.Handle(command, CancellationToken.None);

        Assert.Empty(periods.Periods);
        Assert.Single(transactions.Transactions);
    }

    [Fact]
    public async Task Handle_DebitCardWithInstallments_ThrowsApplicationValidationException()
    {
        var (handler, users, cards, plans, _, transactions) = BuildHandler();
        users.Seed(OwnerUser());
        cards.Seed(DebitCard());

        var command = new RegisterTransactionCommand(
            "user-1", "card-2", "Supermarket", new DateOnly(2026, 3, 10), 5000, Currency.CRC,
            new InstallmentPlanInput(TotalInstallments: 3, InstallmentAmount: 1666.67m),
            [new PersonShare("user-1", 100)]);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => handler.Handle(command, CancellationToken.None));

        Assert.Empty(plans.Plans);
        Assert.Empty(transactions.Transactions);
    }

    [Fact]
    public async Task Handle_RestrictedViewerRole_ThrowsForbidden()
    {
        var (handler, users, cards, _, _, _) = BuildHandler();
        users.Seed(RestrictedViewerUser());
        cards.Seed(CreditCard());

        var command = new RegisterTransactionCommand(
            "user-2", "card-1", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null,
            [new PersonShare("user-2", 100)]);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownCard_ThrowsNotFound()
    {
        var (handler, users, _, _, _, _) = BuildHandler();
        users.Seed(OwnerUser());

        var command = new RegisterTransactionCommand(
            "user-1", "missing-card", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null,
            [new PersonShare("user-1", 100)]);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownActingUser_ThrowsNotFound()
    {
        var (handler, _, cards, _, _, _) = BuildHandler();
        cards.Seed(CreditCard());

        var command = new RegisterTransactionCommand(
            "missing-user", "card-1", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null,
            [new PersonShare("missing-user", 100)]);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SplitDoesNotSumTo100_ThrowsDomainException()
    {
        var (handler, users, cards, _, _, _) = BuildHandler();
        users.Seed(OwnerUser());
        cards.Seed(CreditCard());

        var command = new RegisterTransactionCommand(
            "user-1", "card-1", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null,
            [new PersonShare("user-1", 60)]);

        await Assert.ThrowsAsync<SplitCard.Domain.Exceptions.DomainException>(() => handler.Handle(command, CancellationToken.None));
    }
}
