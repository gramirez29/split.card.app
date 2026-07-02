using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence.Mappings;

internal static class TransactionMappings
{
    public static TransactionDocument ToDocument(this Transaction transaction) =>
        new()
        {
            Id = transaction.Id,
            CardId = transaction.CardId,
            Merchant = transaction.Merchant,
            PurchaseDate = transaction.PurchaseDate,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            InstallmentPlanId = transaction.InstallmentPlanId,
            CreatedByUserId = transaction.CreatedByUserId,
            Split = transaction.Split.Select(s => s.ToDocument()).ToList()
        };

    public static Transaction ToDomain(this TransactionDocument document) =>
        new(
            document.Id,
            document.CardId,
            document.Merchant,
            document.PurchaseDate,
            document.Amount,
            document.Currency,
            document.InstallmentPlanId,
            document.CreatedByUserId,
            document.Split.Select(s => s.ToDomain()).ToList());
}
