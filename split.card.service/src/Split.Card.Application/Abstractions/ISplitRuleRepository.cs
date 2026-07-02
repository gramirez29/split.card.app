using SplitCard.Domain.Entities;

namespace SplitCard.Application.Abstractions;

public interface ISplitRuleRepository
{
    Task<IReadOnlyList<SplitRule>> GetByHouseholdIdAsync(string householdId, CancellationToken cancellationToken);
    Task AddAsync(SplitRule splitRule, CancellationToken cancellationToken);
}
