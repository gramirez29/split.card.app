using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace SplitCard.Application.Households;

public sealed record RegisterHouseholdCommand(
    string HouseholdName,
    string OwnerName,
    string OwnerEmail,
    string OwnerPassword);

public sealed record RegisterHouseholdResult(string HouseholdId, string OwnerUserId);

/// <summary>
/// Bootstraps a new Household with its first Owner user. This is the entry point for a
/// brand-new SplitCard account — there is no separate "sign up" concept, registering a
/// household and registering its Owner happen atomically.
/// </summary>
public sealed class RegisterHouseholdCommandHandler(
    IHouseholdRepository householdRepository,
    IUserRepository userRepository,
    IPasswordHasher passwordHasher)
{
    public async Task<RegisterHouseholdResult> Handle(RegisterHouseholdCommand command, CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByEmailAsync(command.OwnerEmail, cancellationToken);

        if (existingUser is not null)
        {
            throw new ApplicationValidationException($"Email {command.OwnerEmail} is already registered.");
        }

        var householdId = IdGenerator.NewId();
        var ownerId = IdGenerator.NewId();

        var household = new Household(householdId, command.HouseholdName, [ownerId]);
        var passwordHash = passwordHasher.Hash(command.OwnerPassword);
        var owner = new User(ownerId, householdId, command.OwnerName, command.OwnerEmail, passwordHash, UserRole.Owner);

        await householdRepository.AddAsync(household, cancellationToken);
        await userRepository.AddAsync(owner, cancellationToken);

        return new RegisterHouseholdResult(householdId, ownerId);
    }
}
