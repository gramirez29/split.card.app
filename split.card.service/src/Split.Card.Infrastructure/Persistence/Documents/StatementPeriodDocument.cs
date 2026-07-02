using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SplitCard.Domain.Enums;

namespace SplitCard.Infrastructure.Persistence.Documents;

public sealed class StatementPeriodDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string CardId { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly PaymentDueDate { get; set; }

    [BsonRepresentation(BsonType.String)]
    public StatementStatus Status { get; set; }
}
