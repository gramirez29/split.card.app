using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence.Mappings;

internal static class StatementPeriodMappings
{
    public static StatementPeriodDocument ToDocument(this StatementPeriod period) =>
        new()
        {
            Id = period.Id,
            CardId = period.CardId,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            PaymentDueDate = period.PaymentDueDate,
            Status = period.Status
        };

    public static StatementPeriod ToDomain(this StatementPeriodDocument document)
    {
        var period = new StatementPeriod(document.Id, document.CardId, document.StartDate, document.EndDate, document.PaymentDueDate);
        period.RestoreStatus(document.Status);
        return period;
    }
}
