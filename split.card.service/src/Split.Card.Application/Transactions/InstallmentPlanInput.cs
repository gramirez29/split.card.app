namespace SplitCard.Application.Transactions;

public sealed record InstallmentPlanInput(int TotalInstallments, decimal InstallmentAmount);
