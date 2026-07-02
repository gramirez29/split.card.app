using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.Exceptions;
using Xunit;

namespace Split.Card.Domain.Tests;

public class CardTests
{
    private static SplitCard.Domain.Entities.Card CreditCard(int cutoffDay, int paymentDueDay) =>
        new("card-1", "household-1", "Test Card", "Test Bank", CardType.Credit, cutoffDay, paymentDueDay, "user-1");

    [Fact]
    public void Constructor_CreditCardWithoutCutoffOrDueDay_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new SplitCard.Domain.Entities.Card("id", "hh", "n", "b", CardType.Credit, null, null, "owner"));
    }

    [Fact]
    public void Constructor_DebitCardWithCutoffDay_Throws()
    {
        Assert.Throws<DomainException>(() =>
            new SplitCard.Domain.Entities.Card("id", "hh", "n", "b", CardType.Debit, 15, null, "owner"));
    }

    [Fact]
    public void GetStatementPeriodFor_DebitCard_Throws()
    {
        var card = new SplitCard.Domain.Entities.Card("id", "hh", "n", "b", CardType.Debit, null, null, "owner");

        Assert.Throws<DomainException>(() => card.GetStatementPeriodFor(new DateOnly(2026, 1, 15)));
    }

    [Fact]
    public void GetStatementPeriodFor_PurchaseBeforeCutoff_BelongsToCurrentMonthCutoff()
    {
        var card = CreditCard(cutoffDay: 20, paymentDueDay: 5);

        var period = card.GetStatementPeriodFor(new DateOnly(2026, 3, 10));

        Assert.Equal(new DateOnly(2026, 2, 21), period.StartDate);
        Assert.Equal(new DateOnly(2026, 3, 20), period.EndDate);
    }

    [Fact]
    public void GetStatementPeriodFor_PurchaseAfterCutoff_BelongsToNextMonthCutoff()
    {
        var card = CreditCard(cutoffDay: 20, paymentDueDay: 5);

        var period = card.GetStatementPeriodFor(new DateOnly(2026, 3, 25));

        Assert.Equal(new DateOnly(2026, 3, 21), period.StartDate);
        Assert.Equal(new DateOnly(2026, 4, 20), period.EndDate);
    }

    [Fact]
    public void GetStatementPeriodFor_PurchaseOnCutoffDay_IsIncludedInCurrentPeriod()
    {
        var card = CreditCard(cutoffDay: 20, paymentDueDay: 5);

        var period = card.GetStatementPeriodFor(new DateOnly(2026, 3, 20));

        Assert.Equal(new DateOnly(2026, 3, 20), period.EndDate);
    }

    [Fact]
    public void GetStatementPeriodFor_CutoffDay31_ClampsToLastDayOfShortMonth()
    {
        // Cutoff day 31: February only has 28 days in 2026 (not a leap year).
        var card = CreditCard(cutoffDay: 31, paymentDueDay: 10);

        var period = card.GetStatementPeriodFor(new DateOnly(2026, 2, 15));

        Assert.Equal(new DateOnly(2026, 2, 28), period.EndDate);
        Assert.Equal(new DateOnly(2026, 2, 1), period.StartDate);
    }

    [Fact]
    public void GetStatementPeriodFor_PaymentDueDayAfterCutoffDay_FallsInSameMonthAsCutoff()
    {
        var card = CreditCard(cutoffDay: 10, paymentDueDay: 25);

        var period = card.GetStatementPeriodFor(new DateOnly(2026, 3, 5));

        Assert.Equal(new DateOnly(2026, 3, 25), period.PaymentDueDate);
    }

    [Fact]
    public void GetStatementPeriodFor_PaymentDueDayBeforeOrEqualCutoffDay_FallsInFollowingMonth()
    {
        var card = CreditCard(cutoffDay: 20, paymentDueDay: 5);

        var period = card.GetStatementPeriodFor(new DateOnly(2026, 3, 25));

        Assert.Equal(new DateOnly(2026, 5, 5), period.PaymentDueDate);
    }
}
