using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace SplitCard.Application.SplitRules;

public sealed record CreateSplitRuleCommand(
    string ActingUserId,
    string HouseholdId,
    string DescriptionPattern,
    IReadOnlyList<PersonShare> DefaultSplit);

public sealed class CreateSplitRuleCommandHandler(IUserRepository userRepository, ISplitRuleRepository splitRuleRepository)
{
    public async Task<SplitRule> Handle(CreateSplitRuleCommand command, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(command.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {command.ActingUserId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, command.HouseholdId);

        if (actingUser.Role != UserRole.Owner)
        {
            throw new ForbiddenException("Only the household Owner can create split rules.");
        }

        var rule = new SplitRule(IdGenerator.NewId(), command.HouseholdId, command.DescriptionPattern, command.DefaultSplit);

        await splitRuleRepository.AddAsync(rule, cancellationToken);

        return rule;
    }
}
