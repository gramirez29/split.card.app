using SplitCard.Domain.Common;
using SplitCard.Domain.Exceptions;
using SplitCard.Domain.ValueObjects;

namespace SplitCard.Domain.Entities;

public sealed class SplitRule : Entity
{
    public string HouseholdId { get; private set; }
    public string DescriptionPattern { get; private set; }
    public IReadOnlyList<PersonShare> DefaultSplit { get; private set; }

    private SplitRule()
    {
        HouseholdId = string.Empty;
        DescriptionPattern = string.Empty;
        DefaultSplit = [];
    }

    public SplitRule(string id, string householdId, string descriptionPattern, IReadOnlyList<PersonShare> defaultSplit)
    {
        if (string.IsNullOrWhiteSpace(descriptionPattern))
        {
            throw new DomainException("SplitRule.DescriptionPattern is required.");
        }

        PersonShare.EnsureValidSplit(defaultSplit);

        Id = id;
        HouseholdId = householdId;
        DescriptionPattern = descriptionPattern;
        DefaultSplit = defaultSplit;
    }

    public bool Matches(string merchant) =>
        merchant.Contains(DescriptionPattern, StringComparison.OrdinalIgnoreCase);
}
