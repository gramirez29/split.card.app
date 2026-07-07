using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Common;
using SplitCard.Application.SplitRules;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Application.Tests;

public class CreateSplitRuleCommandHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    private static User ContributorUser(string id, string householdId) =>
        new(id, householdId, "Contributor", $"{id}@test.com", "hash", UserRole.Contributor);

    [Fact]
    public async Task Handle_OwnerSameHousehold_CreatesRule()
    {
        var users = new InMemoryUserRepository();
        var rules = new InMemorySplitRuleRepository();
        users.Seed(OwnerUser("user-1", "household-1"));

        var handler = new CreateSplitRuleCommandHandler(users, rules);

        var command = new CreateSplitRuleCommand("user-1", "household-1", "Netflix", [new PersonShare("user-1", 100)]);
        var rule = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("household-1", rule.HouseholdId);
    }

    [Fact]
    public async Task Handle_DifferentHousehold_ThrowsForbidden()
    {
        var users = new InMemoryUserRepository();
        var rules = new InMemorySplitRuleRepository();
        users.Seed(OwnerUser("user-1", "household-1"));

        var handler = new CreateSplitRuleCommandHandler(users, rules);

        var command = new CreateSplitRuleCommand("user-1", "household-2", "Netflix", [new PersonShare("user-1", 100)]);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Empty(rules.Rules);
    }

    [Fact]
    public async Task Handle_ContributorRole_ThrowsForbidden()
    {
        var users = new InMemoryUserRepository();
        var rules = new InMemorySplitRuleRepository();
        users.Seed(ContributorUser("user-1", "household-1"));

        var handler = new CreateSplitRuleCommandHandler(users, rules);

        var command = new CreateSplitRuleCommand("user-1", "household-1", "Netflix", [new PersonShare("user-1", 100)]);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }
}
