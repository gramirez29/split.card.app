using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.ValueObjects;

namespace SplitCard.Application.SplitRules;

public sealed record SuggestSplitForMerchantQuery(string HouseholdId, string Merchant, string ActingUserId);

/// <summary>
/// Backs the "autocomplete split from a recurring purchase" idea discussed for the mobile
/// capture flow: given a merchant name typed at the moment of purchase, returns the first
/// matching SplitRule's DefaultSplit, or null if nothing matches (caller falls back to
/// manual split entry). Match is case-insensitive substring (SplitRule.Matches).
/// </summary>
public sealed class SuggestSplitForMerchantQueryHandler(
    ISplitRuleRepository splitRuleRepository,
    IUserRepository userRepository)
{
    public async Task<IReadOnlyList<PersonShare>?> Handle(SuggestSplitForMerchantQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, query.HouseholdId);

        var rules = await splitRuleRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
        var match = rules.FirstOrDefault(r => r.Matches(query.Merchant));

        return match?.DefaultSplit;
    }
}
