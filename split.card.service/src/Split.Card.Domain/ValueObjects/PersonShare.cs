using SplitCard.Domain.Exceptions;

namespace SplitCard.Domain.ValueObjects;

public sealed record PersonShare
{
    public string PersonId { get; }
    public decimal Percentage { get; }

    public PersonShare(string personId, decimal percentage)
    {
        if (string.IsNullOrWhiteSpace(personId))
        {
            throw new DomainException("PersonShare requires a valid PersonId.");
        }

        if (percentage <= 0 || percentage > 100)
        {
            throw new DomainException("PersonShare.Percentage must be between 0 (exclusive) and 100 (inclusive).");
        }

        PersonId = personId;
        Percentage = percentage;
    }

    public static void EnsureValidSplit(IReadOnlyList<PersonShare> split)
    {
        if (split is null || split.Count == 0)
        {
            throw new DomainException("A transaction requires at least one PersonShare.");
        }

        var hasDuplicates = split
            .GroupBy(s => s.PersonId)
            .Any(g => g.Count() > 1);

        if (hasDuplicates)
        {
            throw new DomainException("A PersonId cannot appear more than once in the same split.");
        }

        var totalPercentage = split.Sum(s => s.Percentage);

        if (totalPercentage != 100)
        {
            throw new DomainException($"Split percentages must sum to 100. Current sum: {totalPercentage}.");
        }
    }
}
