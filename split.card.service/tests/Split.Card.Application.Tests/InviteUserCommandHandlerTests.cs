using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Common;
using SplitCard.Application.Users;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace Split.Card.Application.Tests;

public class InviteUserCommandHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    private static User ContributorUser(string id, string householdId) =>
        new(id, householdId, "Contributor", $"{id}@test.com", "hash", UserRole.Contributor);

    private static (InviteUserCommandHandler Handler, InMemoryUserRepository Users, InMemoryHouseholdRepository Households) BuildHandler()
    {
        var users = new InMemoryUserRepository();
        var households = new InMemoryHouseholdRepository();
        var handler = new InviteUserCommandHandler(users, households, new FakePasswordHasher());

        return (handler, users, households);
    }

    [Fact]
    public async Task Handle_OwnerSameHousehold_InvitesUser()
    {
        var (handler, users, households) = BuildHandler();
        users.Seed(OwnerUser("user-1", "household-1"));
        households.Seed(new Household("household-1", "Test Household", ["user-1"]));

        var command = new InviteUserCommand("user-1", "household-1", "New Person", "new@test.com", "password123", UserRole.Contributor);
        var newUser = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("household-1", newUser.HouseholdId);
    }

    [Fact]
    public async Task Handle_DifferentHousehold_ThrowsForbidden()
    {
        var (handler, users, households) = BuildHandler();
        users.Seed(OwnerUser("user-1", "household-1"));
        households.Seed(new Household("household-2", "Otro Hogar", ["user-in-household-2"]));

        var command = new InviteUserCommand("user-1", "household-2", "New Person", "new@test.com", "password123", UserRole.Contributor);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ContributorActingUser_ThrowsForbidden()
    {
        var (handler, users, households) = BuildHandler();
        users.Seed(ContributorUser("user-1", "household-1"));
        households.Seed(new Household("household-1", "Test Household", ["user-1"]));

        var command = new InviteUserCommand("user-1", "household-1", "New Person", "new@test.com", "password123", UserRole.RestrictedViewer);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InviteAnotherOwner_ThrowsApplicationValidationException()
    {
        var (handler, users, households) = BuildHandler();
        users.Seed(OwnerUser("user-1", "household-1"));
        households.Seed(new Household("household-1", "Test Household", ["user-1"]));

        var command = new InviteUserCommand("user-1", "household-1", "Second Owner", "owner2@test.com", "password123", UserRole.Owner);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsApplicationValidationException()
    {
        var (handler, users, households) = BuildHandler();
        users.Seed(OwnerUser("user-1", "household-1"));
        users.Seed(new User("user-2", "household-1", "Existing", "existing@test.com", "hash", UserRole.Contributor));
        households.Seed(new Household("household-1", "Test Household", ["user-1", "user-2"]));

        var command = new InviteUserCommand("user-1", "household-1", "New Person", "existing@test.com", "password123", UserRole.Contributor);

        await Assert.ThrowsAsync<ApplicationValidationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
