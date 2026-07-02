using MongoDB.Bson.Serialization.Attributes;

namespace SplitCard.Infrastructure.Persistence.Documents;

public sealed class SplitRuleDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string HouseholdId { get; set; } = string.Empty;
    public string DescriptionPattern { get; set; } = string.Empty;
    public List<PersonShareDocument> DefaultSplit { get; set; } = [];
}
