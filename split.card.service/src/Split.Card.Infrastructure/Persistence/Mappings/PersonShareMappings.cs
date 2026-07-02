using SplitCard.Domain.ValueObjects;
using SplitCard.Infrastructure.Persistence.Documents;

namespace SplitCard.Infrastructure.Persistence.Mappings;

internal static class PersonShareMappings
{
    public static PersonShareDocument ToDocument(this PersonShare share) =>
        new()
        {
            PersonId = share.PersonId,
            Percentage = share.Percentage
        };

    public static PersonShare ToDomain(this PersonShareDocument document) =>
        new(document.PersonId, document.Percentage);
}
