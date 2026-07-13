using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Domain.Tests;

public class TransactionTests
{
    private static SplitCard.Domain.Entities.Transaction OneTimeTransaction(decimal amount = 1000m) =>
        new("tx-1", "card-1", "Tienda", new DateOnly(2026, 3, 10), amount, Currency.CRC, null, "user-1",
            [new PersonShare("user-1", 60), new PersonShare("user-2", 40)]);

    private static SplitCard.Domain.Entities.Transaction InstallmentTransaction(decimal totalAmount = 12000m) =>
        new("tx-2", "card-1", "PriceSmart", new DateOnly(2026, 3, 10), totalAmount, Currency.CRC, "plan-1", "user-1",
            [new PersonShare("user-1", 100)]);

    private static InstallmentPlan Plan(int totalInstallments = 3, decimal installmentAmount = 4000m) =>
        InstallmentPlan.CreateForNewPurchase("plan-1", totalInstallments, installmentAmount, Currency.CRC, new DateOnly(2026, 3, 10));

    [Fact]
    public void GetPeriodAmount_NoInstallmentPlan_ReturnsFullAmount()
    {
        var transaction = OneTimeTransaction(1000m);

        Assert.Equal(1000m, transaction.GetPeriodAmount(null));
    }

    [Fact]
    public void GetPeriodAmount_WithInstallmentPlan_ReturnsInstallmentAmount_NotFullTotal()
    {
        var transaction = InstallmentTransaction(totalAmount: 12000m);
        var plan = Plan(totalInstallments: 3, installmentAmount: 4000m);

        var periodAmount = transaction.GetPeriodAmount(plan);

        Assert.Equal(4000m, periodAmount);
        Assert.NotEqual(transaction.Amount, periodAmount);
    }

    [Fact]
    public void GetShareAmount_SplitsTheGivenAmount_NotTransactionAmount()
    {
        var transaction = OneTimeTransaction(1000m);

        // Pasar un amountForPeriod distinto de Amount debe reflejarse en el resultado —
        // confirma que el método usa el parámetro, no Amount internamente por error.
        var shareFromPeriodAmount = transaction.GetShareAmount("user-1", amountForPeriod: 4000m);

        Assert.Equal(2400m, shareFromPeriodAmount); // 60% de 4000, no 60% de 1000
    }

    [Fact]
    public void GetShareAmount_PersonNotInSplit_ReturnsZero()
    {
        var transaction = OneTimeTransaction(1000m);

        Assert.Equal(0m, transaction.GetShareAmount("someone-else", amountForPeriod: 1000m));
    }
}
