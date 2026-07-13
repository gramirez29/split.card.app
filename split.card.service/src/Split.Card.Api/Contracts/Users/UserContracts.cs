using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace SplitCard.Api.Contracts.Users;

public sealed record InviteUserRequest(
    string HouseholdId,
    string Name,
    string Email,
    string Password,
    UserRole Role);

// Deliberately excludes PasswordHash — never serialize it, even internally-shaped.
public sealed record UserResponse(string Id, string HouseholdId, string Name, string Email, UserRole Role)
{
    public static UserResponse FromDomain(User user) =>
        new(user.Id, user.HouseholdId, user.Name, user.Email, user.Role);
}
