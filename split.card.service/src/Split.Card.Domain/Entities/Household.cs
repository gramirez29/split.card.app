using SplitCard.Domain.Common;
using SplitCard.Domain.Exceptions;

namespace SplitCard.Domain.Entities;

public sealed class Household : Entity
{
    private readonly List<string> _memberUserIds = [];

    public string Name { get; private set; }
    public IReadOnlyCollection<string> MemberUserIds => _memberUserIds.AsReadOnly();

    private Household()
    {
        Name = string.Empty;
    }

    public Household(string id, string name, IEnumerable<string> memberUserIds)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Household.Name is required.");
        }

        Id = id;
        Name = name;
        _memberUserIds = memberUserIds.ToList();
    }

    public void AddMember(string userId)
    {
        if (_memberUserIds.Contains(userId))
        {
            throw new DomainException($"User {userId} is already a member of this household.");
        }

        _memberUserIds.Add(userId);
    }

    public void RemoveMember(string userId)
    {
        if (!_memberUserIds.Remove(userId))
        {
            throw new DomainException($"User {userId} is not a member of this household.");
        }
    }
}
