using SplitCard.Domain.Common;
using SplitCard.Domain.Enums;
using SplitCard.Domain.Exceptions;

namespace SplitCard.Domain.Entities;

public sealed class StatementPeriod : Entity
{
    public string CardId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public DateOnly PaymentDueDate { get; private set; }
    public StatementStatus Status { get; private set; }

    private StatementPeriod()
    {
        CardId = string.Empty;
    }

    public StatementPeriod(string id, string cardId, DateOnly startDate, DateOnly endDate, DateOnly paymentDueDate)
    {
        if (endDate <= startDate)
        {
            throw new DomainException("StatementPeriod.EndDate must be after StartDate.");
        }

        Id = id;
        CardId = cardId;
        StartDate = startDate;
        EndDate = endDate;
        PaymentDueDate = paymentDueDate;
        Status = StatementStatus.Open;
    }

    public void Close()
    {
        if (Status != StatementStatus.Open)
        {
            throw new DomainException($"Cannot close a StatementPeriod in status {Status}.");
        }

        Status = StatementStatus.Closed;
    }

    public void MarkAsPaid()
    {
        if (Status != StatementStatus.Closed)
        {
            throw new DomainException($"Cannot mark as paid a StatementPeriod in status {Status}.");
        }

        Status = StatementStatus.Paid;
    }

    internal void RestoreStatus(StatementStatus status)
    {
        Status = status;
    }
}
