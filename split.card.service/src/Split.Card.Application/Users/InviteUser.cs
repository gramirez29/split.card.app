using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace SplitCard.Application.Users;

public sealed record InviteUserCommand(
    string ActingUserId,
    string HouseholdId,
    string Name,
    string Email,
    string Password,
    UserRole Role);

/// <summary>
/// Adds a Contributor or RestrictedViewer to an existing Household. Only the Owner can
/// invite members. A second Owner cannot be created through this path.
/// </summary>
public sealed class InviteUserCommandHandler(
    IUserRepository userRepository,
    IHouseholdRepository householdRepository,
    IPasswordHasher passwordHasher)
{
    public async Task<User> Handle(InviteUserCommand command, CancellationToken cancellationToken)
    {
        if (command.Role == UserRole.Owner)
        {
            throw new ApplicationValidationException("Cannot invite a second Owner into a household.");
        }

        var actingUser = await userRepository.GetByIdAsync(command.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {command.ActingUserId} not found.");

        if (actingUser.Role != UserRole.Owner)
        {
            throw new ForbiddenException("Only the household Owner can invite new members.");
        }

        var household = await householdRepository.GetByIdAsync(command.HouseholdId, cancellationToken)
            ?? throw new NotFoundException($"Household {command.HouseholdId} not found.");

        var existingUser = await userRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (existingUser is not null)
        {
            throw new ApplicationValidationException($"Email {command.Email} is already registered.");
        }

        var newUserId = IdGenerator.NewId();
        var passwordHash = passwordHasher.Hash(command.Password);
        var newUser = new User(newUserId, command.HouseholdId, command.Name, command.Email, passwordHash, command.Role);

        household.AddMember(newUserId);

        await userRepository.AddAsync(newUser, cancellationToken);
        await householdRepository.UpdateAsync(household, cancellationToken);

        return newUser;
    }
}
