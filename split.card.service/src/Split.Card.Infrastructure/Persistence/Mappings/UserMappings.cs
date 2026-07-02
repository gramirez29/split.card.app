using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence.Mappings;

internal static class UserMappings
{
    public static UserDocument ToDocument(this User user) =>
        new()
        {
            Id = user.Id,
            HouseholdId = user.HouseholdId,
            Name = user.Name,
            Email = user.Email,
            PasswordHash = user.PasswordHash,
            Role = user.Role
        };

    public static User ToDomain(this UserDocument document) =>
        new(document.Id, document.HouseholdId, document.Name, document.Email, document.PasswordHash, document.Role);
}
