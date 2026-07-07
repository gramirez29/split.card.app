using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Common;
using SplitCard.Application.SplitRules;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace Split.Card.Application.Tests;

public class GetSplitRulesForHouseholdQueryHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    [Fact]
    public async Task Handle_SameHousehold_ReturnsRules()
    {
        var users = new InMemoryUserRepository();
        var rules = new InMemorySplitRuleRepository();
        users.Seed(OwnerUser("user-1", "household-1"));
        rules.Rules.Add(new SplitRule("rule-1", "household-1", "Netflix", [new PersonShare("user-1", 100)]));

        var handler = new GetSplitRulesForHouseholdQueryHandler(rules, users);

        var result = await handler.Handle(new GetSplitRulesForHouseholdQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_DifferentHousehold_ThrowsForbidden()
    {
        var users = new InMemoryUserRepository();
        var rules = new InMemorySplitRuleRepository();
        users.Seed(OwnerUser("user-1", "household-1"));
        rules.Rules.Add(new SplitRule("rule-1", "household-2", "Ajena", [new PersonShare("user-in-household-2", 100)]));

        var handler = new GetSplitRulesForHouseholdQueryHandler(rules, users);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetSplitRulesForHouseholdQuery("household-2", "user-1"), CancellationToken.None));
    }
}
