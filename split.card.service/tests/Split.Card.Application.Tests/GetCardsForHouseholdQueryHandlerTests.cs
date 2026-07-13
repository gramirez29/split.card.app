using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Cards;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace Split.Card.Application.Tests;

public class GetCardsForHouseholdQueryHandlerTests
{
    private static User OwnerUser(string id, string householdId) =>
        new(id, householdId, "Owner", $"{id}@test.com", "hash", UserRole.Owner);

    [Fact]
    public async Task Handle_SameHousehold_ReturnsCards()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(new SplitCard.Domain.Entities.Card("card-1", "household-1", "Test Card", "Test Bank", CardType.Credit, 20, 5, "user-1"));

        var handler = new GetCardsForHouseholdQueryHandler(cards, users);

        var result = await handler.Handle(new GetCardsForHouseholdQuery("household-1", "user-1"), CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_DifferentHousehold_ThrowsForbidden()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();
        users.Seed(OwnerUser("user-1", "household-1"));
        cards.Seed(new SplitCard.Domain.Entities.Card("card-1", "household-2", "Ajena", "Otro Banco", CardType.Credit, 20, 5, "user-in-household-2"));

        var handler = new GetCardsForHouseholdQueryHandler(cards, users);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => handler.Handle(new GetCardsForHouseholdQuery("household-2", "user-1"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownActingUser_ThrowsNotFound()
    {
        var users = new InMemoryUserRepository();
        var cards = new InMemoryCardRepository();

        var handler = new GetCardsForHouseholdQueryHandler(cards, users);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetCardsForHouseholdQuery("household-1", "missing-user"), CancellationToken.None));
    }
}
