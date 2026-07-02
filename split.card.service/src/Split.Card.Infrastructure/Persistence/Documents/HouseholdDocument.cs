using MongoDB.Bson.Serialization.Attributes;

namespace SplitCard.Infrastructure.Persistence.Documents;

public sealed class HouseholdDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public List<string> MemberUserIds { get; set; } = [];
}
