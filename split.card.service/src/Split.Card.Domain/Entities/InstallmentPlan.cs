using SplitCard.Domain.Common;
using SplitCard.Domain.Enums;
using SplitCard.Domain.Exceptions;

namespace SplitCard.Domain.Entities;

public sealed class InstallmentPlan : Entity
{
    public int TotalInstallments { get; private set; }
    public decimal InstallmentAmount { get; private set; }
    public Currency Currency { get; private set; }
    public DateOnly FirstChargeDate { get; private set; }

    private InstallmentPlan()
    {
    }

    private InstallmentPlan(string id, int totalInstallments, decimal installmentAmount, Currency currency, DateOnly firstChargeDate)
    {
        if (totalInstallments <= 0)
        {
            throw new DomainException("InstallmentPlan.TotalInstallments must be greater than zero.");
        }

        if (installmentAmount <= 0)
        {
            throw new DomainException("InstallmentPlan.InstallmentAmount must be greater than zero.");
        }

        Id = id;
        TotalInstallments = totalInstallments;
        InstallmentAmount = installmentAmount;
        Currency = currency;
        FirstChargeDate = firstChargeDate;
    }

    /// <summary>
    /// Used when the user registers a brand-new purchase in installments at the moment of purchase.
    /// </summary>
    public static InstallmentPlan CreateForNewPurchase(
        string id,
        int totalInstallments,
        decimal installmentAmount,
        Currency currency,
        DateOnly purchaseDate)
    {
        return new InstallmentPlan(id, totalInstallments, installmentAmount, currency, purchaseDate);
    }

    /// <summary>
    /// Used during onboarding/seed, when the user knows how many installments are already
    /// paid but not the exact original purchase date. Derives an equivalent FirstChargeDate
    /// by walking back AlreadyPaidInstallments months from the reference date (today).
    /// </summary>
    public static InstallmentPlan CreateFromExistingPlan(
        string id,
        int totalInstallments,
        int alreadyPaidInstallments,
        decimal installmentAmount,
        Currency currency,
        DateOnly referenceDate)
    {
        if (alreadyPaidInstallments < 0 || alreadyPaidInstallments >= totalInstallments)
        {
            throw new DomainException("AlreadyPaidInstallments must be between 0 and TotalInstallments - 1.");
        }

        var derivedFirstChargeDate = referenceDate.AddMonths(-alreadyPaidInstallments);

        return new InstallmentPlan(id, totalInstallments, installmentAmount, currency, derivedFirstChargeDate);
    }

    /// <summary>
    /// Used when reconstructing an InstallmentPlan directly from persistence, where
    /// FirstChargeDate is already known precisely.
    /// </summary>
    public static InstallmentPlan Reconstruct(
        string id,
        int totalInstallments,
        decimal installmentAmount,
        Currency currency,
        DateOnly firstChargeDate)
    {
        return new InstallmentPlan(id, totalInstallments, installmentAmount, currency, firstChargeDate);
    }

    public int GetInstallmentNumberFor(DateOnly statementPeriodEndDate)
    {
        var monthsElapsed =
            ((statementPeriodEndDate.Year - FirstChargeDate.Year) * 12) +
            statementPeriodEndDate.Month - FirstChargeDate.Month;

        return monthsElapsed + 1;
    }

    public bool HasActiveInstallmentIn(DateOnly statementPeriodStartDate, DateOnly statementPeriodEndDate)
    {
        var installmentNumber = GetInstallmentNumberFor(statementPeriodEndDate);
        return installmentNumber >= 1 && installmentNumber <= TotalInstallments;
    }
}
