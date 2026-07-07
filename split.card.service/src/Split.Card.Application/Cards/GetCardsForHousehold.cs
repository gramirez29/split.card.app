using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.Cards;

public sealed record GetCardsForHouseholdQuery(string HouseholdId);

/// <summary>
/// No per-role filtering here: every household member can see the list of cards
/// (name/bank/cutoff), only Transaction visibility is restricted by Split[].PersonId.
/// </summary>
public sealed class GetCardsForHouseholdQueryHandler(ICardRepository cardRepository)
{
    public Task<IReadOnlyList<Card>> Handle(GetCardsForHouseholdQuery query, CancellationToken cancellationToken) =>
        cardRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
}
