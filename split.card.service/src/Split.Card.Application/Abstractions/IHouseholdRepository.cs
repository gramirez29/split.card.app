using SplitCard.Domain.Entities;

namespace SplitCard.Application.Abstractions;

public interface IHouseholdRepository
{
    Task<Household?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task AddAsync(Household household, CancellationToken cancellationToken);
    Task UpdateAsync(Household household, CancellationToken cancellationToken);
}
