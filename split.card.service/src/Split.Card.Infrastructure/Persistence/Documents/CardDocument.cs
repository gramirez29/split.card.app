using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SplitCard.Domain.Enums;

namespace SplitCard.Infrastructure.Persistence.Documents;

public sealed class CardDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string HouseholdId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public CardType Type { get; set; }

    public int? CutoffDay { get; set; }
    public int? PaymentDueDay { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
}
