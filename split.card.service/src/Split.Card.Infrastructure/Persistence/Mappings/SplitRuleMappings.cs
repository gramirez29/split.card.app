using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence.Mappings;

internal static class SplitRuleMappings
{
    public static SplitRuleDocument ToDocument(this SplitRule rule) =>
        new()
        {
            Id = rule.Id,
            HouseholdId = rule.HouseholdId,
            DescriptionPattern = rule.DescriptionPattern,
            DefaultSplit = rule.DefaultSplit.Select(s => s.ToDocument()).ToList()
        };

    public static SplitRule ToDomain(this SplitRuleDocument document) =>
        new(document.Id, document.HouseholdId, document.DescriptionPattern,
            document.DefaultSplit.Select(s => s.ToDomain()).ToList());
}
