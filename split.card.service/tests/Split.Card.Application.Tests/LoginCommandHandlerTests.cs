using Split.Card.Application.Tests.Fakes;
using SplitCard.Application.Auth;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;

namespace Split.Card.Application.Tests;

public class LoginCommandHandlerTests
{
    private static (LoginCommandHandler Handler, InMemoryUserRepository Users) BuildHandler()
    {
        var users = new InMemoryUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var tokenGenerator = new FakeTokenGenerator();

        var handler = new LoginCommandHandler(users, passwordHasher, tokenGenerator);

        return (handler, users);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokenAndUserId()
    {
        var (handler, users) = BuildHandler();
        var hasher = new FakePasswordHasher();
        users.Seed(new User("user-1", "household-1", "Owner", "owner@test.com", hasher.Hash("correct-password"), UserRole.Owner));

        var result = await handler.Handle(new LoginCommand("owner@test.com", "correct-password"), CancellationToken.None);

        Assert.Equal("user-1", result.UserId);
        Assert.Equal("fake-token-for-user-1", result.Token);
    }

    [Fact]
    public async Task Handle_WrongPassword_ThrowsUnauthorized()
    {
        var (handler, users) = BuildHandler();
        var hasher = new FakePasswordHasher();
        users.Seed(new User("user-1", "household-1", "Owner", "owner@test.com", hasher.Hash("correct-password"), UserRole.Owner));

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand("owner@test.com", "wrong-password"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_UnknownEmail_ThrowsUnauthorized()
    {
        var (handler, _) = BuildHandler();

        await Assert.ThrowsAsync<UnauthorizedException>(
            () => handler.Handle(new LoginCommand("nobody@test.com", "whatever"), CancellationToken.None));
    }
}
