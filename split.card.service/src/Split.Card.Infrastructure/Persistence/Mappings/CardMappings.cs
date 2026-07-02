using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence.Mappings;

internal static class CardMappings
{
    public static CardDocument ToDocument(this Card card) =>
        new()
        {
            Id = card.Id,
            HouseholdId = card.HouseholdId,
            Name = card.Name,
            Bank = card.Bank,
            Type = card.Type,
            CutoffDay = card.CutoffDay,
            PaymentDueDay = card.PaymentDueDay,
            OwnerUserId = card.OwnerUserId
        };

    public static Card ToDomain(this CardDocument document) =>
        new(document.Id, document.HouseholdId, document.Name, document.Bank, document.Type, document.CutoffDay, document.PaymentDueDay, document.OwnerUserId);
}
