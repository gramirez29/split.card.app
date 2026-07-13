using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.Auth;

public sealed record GetCurrentUserQuery(string UserId);

public sealed class GetCurrentUserQueryHandler(IUserRepository userRepository)
{
    public async Task<User> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken) =>
        await userRepository.GetByIdAsync(query.UserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.UserId} not found.");
}
