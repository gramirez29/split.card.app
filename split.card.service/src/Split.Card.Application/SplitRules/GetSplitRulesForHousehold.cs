using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.SplitRules;

public sealed record GetSplitRulesForHouseholdQuery(string HouseholdId);

public sealed class GetSplitRulesForHouseholdQueryHandler(ISplitRuleRepository splitRuleRepository)
{
    public Task<IReadOnlyList<SplitRule>> Handle(GetSplitRulesForHouseholdQuery query, CancellationToken cancellationToken) =>
        splitRuleRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
}
