using SplitCard.Domain.Common;
using SplitCard.Domain.Enums;
using SplitCard.Domain.Exceptions;
using SplitCard.Domain.ValueObjects;

namespace SplitCard.Domain.Entities;

public sealed class Transaction : Entity
{
    public string CardId { get; private set; }
    public string Merchant { get; private set; }
    public DateOnly PurchaseDate { get; private set; }
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }
    public string? InstallmentPlanId { get; private set; }
    public string CreatedByUserId { get; private set; }
    public IReadOnlyList<PersonShare> Split { get; private set; }

    private Transaction()
    {
        CardId = string.Empty;
        Merchant = string.Empty;
        CreatedByUserId = string.Empty;
        Split = [];
    }

    public Transaction(
        string id,
        string cardId,
        string merchant,
        DateOnly purchaseDate,
        decimal amount,
        Currency currency,
        string? installmentPlanId,
        string createdByUserId,
        IReadOnlyList<PersonShare> split)
    {
        if (string.IsNullOrWhiteSpace(merchant))
        {
            throw new DomainException("Transaction.Merchant is required.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Transaction.Amount must be greater than zero.");
        }

        PersonShare.EnsureValidSplit(split);

        Id = id;
        CardId = cardId;
        Merchant = merchant;
        PurchaseDate = purchaseDate;
        Amount = amount;
        Currency = currency;
        InstallmentPlanId = installmentPlanId;
        CreatedByUserId = createdByUserId;
        Split = split;
    }

    /// <summary>
    /// Amount is ALWAYS the full purchase total, even for installment purchases (e.g.
    /// ₡12,000 for a 6-installment purchase) — it never changes after the purchase is
    /// registered. This method resolves what actually applies for a GIVEN period: the
    /// InstallmentPlan's per-installment amount if one is attached, or the full Amount
    /// otherwise. Callers computing "how much is owed this period" must use this, never
    /// Amount directly, for any transaction that might have an InstallmentPlanId.
    /// </summary>
    public decimal GetPeriodAmount(InstallmentPlan? installmentPlan) =>
        installmentPlan is not null ? installmentPlan.InstallmentAmount : Amount;

    /// <summary>
    /// A person's share of a given amount (percentage-based). Takes the amount explicitly
    /// rather than reading Amount internally, specifically so callers are forced to decide
    /// whether they mean the full purchase total or a period-specific installment amount
    /// (see GetPeriodAmount) — there is no "just give me the default" overload on purpose.
    /// </summary>
    public decimal GetShareAmount(string personId, decimal amountForPeriod)
    {
        var share = Split.FirstOrDefault(s => s.PersonId == personId);
        return share is null ? 0 : amountForPeriod * share.Percentage / 100m;
    }

    public bool IsVisibleTo(string userId) => Split.Any(s => s.PersonId == userId);
}
