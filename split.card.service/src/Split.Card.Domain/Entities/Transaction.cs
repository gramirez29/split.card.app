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

    public decimal GetAmountFor(string personId)
    {
        var share = Split.FirstOrDefault(s => s.PersonId == personId);
        return share is null ? 0 : Amount * share.Percentage / 100m;
    }

    public bool IsVisibleTo(string userId) => Split.Any(s => s.PersonId == userId);
}
