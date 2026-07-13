using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.Cards;

public sealed record GetCardsForHouseholdQuery(string HouseholdId, string ActingUserId);

public sealed class GetCardsForHouseholdQueryHandler(ICardRepository cardRepository, IUserRepository userRepository)
{
    public async Task<IReadOnlyList<Card>> Handle(GetCardsForHouseholdQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, query.HouseholdId);

        return await cardRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
    }
}
