using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence.Mappings;

internal static class HouseholdMappings
{
    public static HouseholdDocument ToDocument(this Household household) =>
        new()
        {
            Id = household.Id,
            Name = household.Name,
            MemberUserIds = household.MemberUserIds.ToList()
        };

    public static Household ToDomain(this HouseholdDocument document) =>
        new(document.Id, document.Name, document.MemberUserIds);
}
