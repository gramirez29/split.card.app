using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;

namespace SplitCard.Application.Auth;

public sealed record LoginCommand(string Email, string Password);

public sealed record LoginResult(string Token, string UserId);

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenGenerator tokenGenerator)
{
    public async Task<LoginResult> Handle(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(command.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            // Deliberately the same message for "no such user" and "wrong password" —
            // distinguishing them lets an attacker enumerate registered emails.
            throw new UnauthorizedException("Invalid email or password.");
        }

        var token = tokenGenerator.GenerateToken(user);

        return new LoginResult(token, user.Id);
    }
}
