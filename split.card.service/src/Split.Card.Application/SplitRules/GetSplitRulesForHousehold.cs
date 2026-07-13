using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.SplitRules;

public sealed record GetSplitRulesForHouseholdQuery(string HouseholdId, string ActingUserId);

public sealed class GetSplitRulesForHouseholdQueryHandler(
    ISplitRuleRepository splitRuleRepository,
    IUserRepository userRepository)
{
    public async Task<IReadOnlyList<SplitRule>> Handle(GetSplitRulesForHouseholdQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, query.HouseholdId);

        return await splitRuleRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
    }
}
