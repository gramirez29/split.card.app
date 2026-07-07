using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace SplitCard.Api.Contracts.Cards;

public sealed record CreateCardRequest(
    string HouseholdId,
    string Name,
    string Bank,
    CardType Type,
    int? CutoffDay,
    int? PaymentDueDay);

public sealed record CardResponse(
    string Id,
    string HouseholdId,
    string Name,
    string Bank,
    CardType Type,
    int? CutoffDay,
    int? PaymentDueDay,
    string OwnerUserId)
{
    public static CardResponse FromDomain(Card card) =>
        new(card.Id, card.HouseholdId, card.Name, card.Bank, card.Type, card.CutoffDay, card.PaymentDueDay, card.OwnerUserId);
}
