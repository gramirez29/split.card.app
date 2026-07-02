namespace SplitCard.Domain.Common;

public abstract class Entity
{
    public string Id { get; protected set; } = string.Empty;

    protected Entity()
    {
    }

    protected Entity(string id)
    {
        Id = id;
    }
}
