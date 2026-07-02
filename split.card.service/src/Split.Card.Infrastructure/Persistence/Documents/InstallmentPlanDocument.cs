using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SplitCard.Domain.Enums;

namespace SplitCard.Infrastructure.Persistence.Documents;

public sealed class InstallmentPlanDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public int TotalInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Currency Currency { get; set; }

    public DateOnly FirstChargeDate { get; set; }
}
