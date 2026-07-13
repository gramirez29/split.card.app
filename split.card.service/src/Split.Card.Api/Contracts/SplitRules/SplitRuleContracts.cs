using SplitCard.Api.Contracts.Transactions;
using SplitCard.Domain.Entities;

namespace SplitCard.Api.Contracts.SplitRules;

public sealed record CreateSplitRuleRequest(
    string HouseholdId,
    string DescriptionPattern,
    IReadOnlyList<PersonShareRequest> DefaultSplit);

public sealed record SplitRuleResponse(
    string Id,
    string HouseholdId,
    string DescriptionPattern,
    IReadOnlyList<PersonShareResponse> DefaultSplit)
{
    public static SplitRuleResponse FromDomain(SplitRule rule) =>
        new(rule.Id, rule.HouseholdId, rule.DescriptionPattern, rule.DefaultSplit.Select(PersonShareResponse.FromDomain).ToList());
}
