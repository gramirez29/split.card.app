using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Common;
using SplitCard.Application.Users;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace Split.Card.Application.Tests;

public class GetHouseholdMembersQueryHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    [Fact]
    public async Task Handle_SameHousehold_ReturnsMembers()
    {
        var users = new InMemoryUserRepository();
        users.Seed(OwnerUser("user-1", "household-1"));
        users.Seed(new User("user-2", "household-1", "Contributor", "user-2@test.com", "hash", UserRole.Contributor));

        var handler = new GetHouseholdMembersQueryHandler(users);

        var result = await handler.Handle(new GetHouseholdMembersQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_DifferentHousehold_ThrowsForbidden()
    {
        var users = new InMemoryUserRepository();
        users.Seed(OwnerUser("user-1", "household-1"));
        users.Seed(OwnerUser("user-in-household-2", "household-2"));

        var handler = new GetHouseholdMembersQueryHandler(users);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetHouseholdMembersQuery("household-2", "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownActingUser_ThrowsNotFound()
    {
        var users = new InMemoryUserRepository();

        var handler = new GetHouseholdMembersQueryHandler(users);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetHouseholdMembersQuery("household-1", "missing-user"), CancellationToken.None));
    }
}
