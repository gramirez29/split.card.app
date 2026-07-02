namespace SplitCard.Infrastructure.Persistence.Documents;

public sealed class PersonShareDocument
{
    public string PersonId { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
}
