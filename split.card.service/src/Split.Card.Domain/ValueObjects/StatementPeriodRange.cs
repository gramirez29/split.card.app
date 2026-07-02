namespace SplitCard.Domain.ValueObjects;

public sealed record StatementPeriodRange(DateOnly StartDate, DateOnly EndDate, DateOnly PaymentDueDate)
{
    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;
}
