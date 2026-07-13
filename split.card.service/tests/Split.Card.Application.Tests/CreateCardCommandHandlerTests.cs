using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Cards;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace Split.Card.Application.Tests;

public class CreateCardCommandHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    private static User ContributorUser(string id, string householdId) =>
        new(id, householdId, "Contributor", $"{id}@test.com", "hash", UserRole.Contributor);

    [Fact]
    public async Task Handle_OwnerSameHousehold_CreatesCard()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        users.Seed(OwnerUser("user-1", "household-1"));

        var handler = new CreateCardCommandHandler(cards, users);

        var command = new CreateCardCommand("user-1", "household-1", "Test Card", "Test Bank", CardType.Credit, 20, 5);
        var card = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("household-1", card.HouseholdId);
    }

    [Fact]
    public async Task Handle_DifferentHousehold_ThrowsForbidden()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        users.Seed(OwnerUser("user-1", "household-1"));

        var handler = new CreateCardCommandHandler(cards, users);

        var command = new CreateCardCommand("user-1", "household-2", "Test Card", "Test Bank", CardType.Credit, 20, 5);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Empty(cards.GetByHouseholdIdAsync("household-2", CancellationToken.None).Result);
    }

    [Fact]
    public async Task Handle_ContributorRole_ThrowsForbidden()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        users.Seed(ContributorUser("user-1", "household-1"));

        var handler = new CreateCardCommandHandler(cards, users);

        var command = new CreateCardCommand("user-1", "household-1", "Test Card", "Test Bank", CardType.Credit, 20, 5);

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownActingUser_ThrowsNotFound()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();

        var handler = new CreateCardCommandHandler(cards, users);

        var command = new CreateCardCommand("missing-user", "household-1", "Test Card", "Test Bank", CardType.Credit, 20, 5);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }
}
