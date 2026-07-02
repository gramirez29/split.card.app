using SplitCard.Domain.Entities;

namespace SplitCard.Application.Abstractions;

public interface IInstallmentPlanRepository
{
    Task<InstallmentPlan?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task AddAsync(InstallmentPlan installmentPlan, CancellationToken cancellationToken);
}
