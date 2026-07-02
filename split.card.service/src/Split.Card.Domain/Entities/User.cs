using SplitCard.Domain.Common;
using SplitCard.Domain.Enums;
using SplitCard.Domain.Exceptions;

namespace SplitCard.Domain.Entities;

public sealed class User : Entity
{
    public string HouseholdId { get; private set; }
    public string Name { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }

    private User()
    {
        HouseholdId = string.Empty;
        Name = string.Empty;
        Email = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string id, string householdId, string name, string email, string passwordHash, UserRole role)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("User.Email is required.");
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("User.PasswordHash is required.");
        }

        Id = id;
        HouseholdId = householdId;
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public bool CanWrite() => Role is UserRole.Owner or UserRole.Contributor;

    public bool CanReadAll() => Role == UserRole.Owner;
}
