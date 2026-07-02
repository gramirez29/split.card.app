using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using SplitCard.Domain.Enums;

namespace SplitCard.Infrastructure.Persistence.Documents;

public sealed class UserDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    public string HouseholdId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.String)]
    public UserRole Role { get; set; }
}
