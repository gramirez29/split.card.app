using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Application.Reconciliation;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Application.Tests;

public class GenerateStatementPdfQueryHandlerTests
{
    private sealed class FakeStatementPdfGenerator : IStatementPdfGenerator
    {
        public StatementPdfModel? LastModel { get; private set; }

        public byte[] Generate(StatementPdfModel model)
        {
            LastModel = model;
            return [0x25, 0x50, 0x44, 0x46]; // "%PDF" magic bytes, fake content is fine here.
        }
    }

    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    private static User RestrictedViewerUser(string id, string householdId) =>
        new(id, householdId, "Kid", $"{id}@test.com", "hash", UserRole.RestrictedViewer);

    private static SplitCard.Domain.Entities.Card CreditCard(string id, string householdId) =>
        new(id, householdId, "Test Card", "Test Bank", CardType.Credit, 20, 5, "owner-id");

    private static (
        GenerateStatementPdfQueryHandler Handler,
        InMemoryUserRepository Users,
        InMemoryCardRepository Cards,
        InMemoryStatementPeriodRepository Periods,
        InMemoryTransactionRepository Transactions,
        InMemoryInstallmentPlanRepository Plans,
        FakeStatementPdfGenerator PdfGenerator) BuildHandler()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        var periods = new InMemoryStatementPeriodRepository();
        var transactions = new InMemoryTransactionRepository();
        var plans = new InMemoryInstallmentPlanRepository();
        var pdfGenerator = new FakeStatementPdfGenerator();

        var handler = new GenerateStatementPdfQueryHandler(users, cards, periods, transactions, plans, pdfGenerator);

        return (handler, users, cards, periods, transactions, plans, pdfGenerator);
    }

    [Fact]
    public async Task Handle_OwnerSameHousehold_GeneratesPdfWithAllTransactions()
    {
        var (handler, users, cards, periods, transactions, _, pdfGenerator) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", new DateOnly(2026, 2, 21), new DateOnly(2026, 3, 20), new DateOnly(2026, 4, 5)));
        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 100)]));

        var result = await handler.Handle(new GenerateStatementPdfQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None);

        Assert.NotEmpty(result);
        Assert.Single(pdfGenerator.LastModel!.Transactions);
        Assert.Equal("Test Card", pdfGenerator.LastModel!.Card.Name);
    }

    [Fact]
    public async Task Handle_InstallmentPurchase_UsesPeriodAmountNotFullTotal()
    {
        var (handler, users, cards, periods, transactions, plans, pdfGenerator) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", new DateOnly(2026, 2, 21), new DateOnly(2026, 3, 20), new DateOnly(2026, 4, 5)));

        var plan = InstallmentPlan.CreateForNewPurchase("plan-1", 3, 4000m, Currency.CRC, new DateOnly(2026, 3, 10));
        plans.Plans.Add(plan);
        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "PriceSmart", new DateOnly(2026, 3, 10), 12000m, Currency.CRC, "plan-1", "user-1",
            [new PersonShare("user-1", 100)]));

        await handler.Handle(new GenerateStatementPdfQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None);

        var resolved = Assert.Single(pdfGenerator.LastModel!.Transactions);
        Assert.Equal(4000m, resolved.PeriodAmount);
        Assert.Equal(1, resolved.InstallmentNumber);
        Assert.Equal(3, resolved.TotalInstallments);
    }

    [Fact]
    public async Task Handle_RestrictedViewer_OnlySeesOwnTransactionsInPdf()
    {
        var (handler, users, cards, periods, transactions, _, pdfGenerator) = BuildHandler();

        users.Seed(RestrictedViewerUser("user-2", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", new DateOnly(2026, 2, 21), new DateOnly(2026, 3, 20), new DateOnly(2026, 4, 5)));

        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "Solo del Owner", new DateOnly(2026, 3, 10), 5000, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 100)]));
        transactions.Transactions.Add(new Transaction(
            "tx-2", "card-1", "Cuota fija hija", new DateOnly(2026, 3, 12), 300, Currency.USD, null, "user-1",
            [new PersonShare("user-2", 100)]));

        await handler.Handle(new GenerateStatementPdfQuery("card-1", new DateOnly(2026, 3, 15), "user-2"), CancellationToken.None);

        var resolved = Assert.Single(pdfGenerator.LastModel!.Transactions);
        Assert.Equal("Cuota fija hija", resolved.Transaction.Merchant);
    }

    [Fact]
    public async Task Handle_CardBelongsToDifferentHousehold_ThrowsForbidden()
    {
        var (handler, users, cards, _, _, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-2"));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GenerateStatementPdfQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NoStatementPeriodForDate_ThrowsNotFound()
    {
        var (handler, users, cards, _, _, _, _) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(CreditCard("card-1", "household-1"));

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GenerateStatementPdfQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_PersonNamesById_ResolvesHouseholdMemberNames()
    {
        var (handler, users, cards, periods, transactions, _, pdfGenerator) = BuildHandler();

        users.Seed(OwnerUser("user-1", "household-1"));
        users.Seed(new User("user-2", "household-1", "María", "maria@test.com", "hash", UserRole.Contributor));
        cards.Seed(CreditCard("card-1", "household-1"));
        periods.Periods.Add(new StatementPeriod("period-1", "card-1", new DateOnly(2026, 2, 21), new DateOnly(2026, 3, 20), new DateOnly(2026, 4, 5)));
        transactions.Transactions.Add(new Transaction(
            "tx-1", "card-1", "Tienda", new DateOnly(2026, 3, 10), 1000, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 50), new PersonShare("user-2", 50)]));

        await handler.Handle(new GenerateStatementPdfQuery("card-1", new DateOnly(2026, 3, 15), "user-1"), CancellationToken.None);

        Assert.Equal("María", pdfGenerator.LastModel!.PersonNamesById["user-2"]);
    }
}
