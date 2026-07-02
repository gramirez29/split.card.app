using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SplitCard.Domain.Enums;

namespace SplitCard.Infrastructure.Persistence.Documents;

public sealed class TransactionDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string CardId { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public DateOnly PurchaseDate { get; set; }
    public decimal Amount { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Currency Currency { get; set; }

    public string? InstallmentPlanId { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public List<PersonShareDocument> Split { get; set; } = [];
}
