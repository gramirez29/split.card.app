using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence.Mappings;

internal static class InstallmentPlanMappings
{
    public static InstallmentPlanDocument ToDocument(this InstallmentPlan plan) =>
        new()
        {
            Id = plan.Id,
            TotalInstallments = plan.TotalInstallments,
            InstallmentAmount = plan.InstallmentAmount,
            Currency = plan.Currency,
            FirstChargeDate = plan.FirstChargeDate
        };

    public static InstallmentPlan ToDomain(this InstallmentPlanDocument document) =>
        InstallmentPlan.Reconstruct(
            document.Id,
            document.TotalInstallments,
            document.InstallmentAmount,
            document.Currency,
            document.FirstChargeDate);
}
