using SplitCard.Domain.Common;
using SplitCard.Domain.Enums;
using SplitCard.Domain.Exceptions;
using SplitCard.Domain.ValueObjects;

namespace SplitCard.Domain.Entities;

public sealed class Card : Entity
{
    public string HouseholdId { get; private set; }
    public string Name { get; private set; }
    public string Bank { get; private set; }
    public CardType Type { get; private set; }
    public int? CutoffDay { get; private set; }
    public int? PaymentDueDay { get; private set; }
    public string OwnerUserId { get; private set; }

    private Card()
    {
        HouseholdId = string.Empty;
        Name = string.Empty;
        Bank = string.Empty;
        OwnerUserId = string.Empty;
    }

    public Card(
        string id,
        string householdId,
        string name,
        string bank,
        CardType type,
        int? cutoffDay,
        int? paymentDueDay,
        string ownerUserId)
    {
        if (type == CardType.Credit && (cutoffDay is null or < 1 or > 31 || paymentDueDay is null or < 1 or > 31))
        {
            throw new DomainException("Credit cards require CutoffDay and PaymentDueDay between 1 and 31.");
        }

        if (type == CardType.Debit && (cutoffDay is not null || paymentDueDay is not null))
        {
            throw new DomainException("Debit cards must not define CutoffDay or PaymentDueDay.");
        }

        Id = id;
        HouseholdId = householdId;
        Name = name;
        Bank = bank;
        Type = type;
        CutoffDay = cutoffDay;
        PaymentDueDay = paymentDueDay;
        OwnerUserId = ownerUserId;
    }

    /// <summary>
    /// Computes the statement period (StartDate/EndDate/PaymentDueDate) that a purchase
    /// made on <paramref name="purchaseDate"/> belongs to, based on this card's CutoffDay
    /// and PaymentDueDay. Heuristic: assumes a fixed cutoff day-of-month, clamped to the
    /// last valid day for months with fewer days (e.g. cutoff 31 in February).
    /// </summary>
    public StatementPeriodRange GetStatementPeriodFor(DateOnly purchaseDate)
    {
        if (Type == CardType.Debit)
        {
            throw new DomainException("Debit card transactions do not belong to a statement period.");
        }

        var cutoffDay = CutoffDay!.Value;
        var dueDay = PaymentDueDay!.Value;

        var currentMonthCutoff = SafeDate(purchaseDate.Year, purchaseDate.Month, cutoffDay);

        DateOnly startDate;
        DateOnly endDate;

        if (purchaseDate <= currentMonthCutoff)
        {
            endDate = currentMonthCutoff;
            var previousMonth = purchaseDate.AddMonths(-1);
            startDate = SafeDate(previousMonth.Year, previousMonth.Month, cutoffDay).AddDays(1);
        }
        else
        {
            startDate = currentMonthCutoff.AddDays(1);
            var nextMonth = purchaseDate.AddMonths(1);
            endDate = SafeDate(nextMonth.Year, nextMonth.Month, cutoffDay);
        }

        var dueMonthOffset = dueDay > cutoffDay ? 0 : 1;
        var dueMonthReference = endDate.AddMonths(dueMonthOffset);
        var paymentDueDate = SafeDate(dueMonthReference.Year, dueMonthReference.Month, dueDay);

        return new StatementPeriodRange(startDate, endDate, paymentDueDate);
    }

    private static DateOnly SafeDate(int year, int month, int day)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Min(day, daysInMonth));
    }
}
