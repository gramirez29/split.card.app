using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.Users;

public sealed record GetHouseholdMembersQuery(string HouseholdId, string ActingUserId);

public sealed class GetHouseholdMembersQueryHandler(IUserRepository userRepository)
{
    public async Task<IReadOnlyList<User>> Handle(GetHouseholdMembersQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, query.HouseholdId);

        return await userRepository.GetByHouseholdIdAsync(query.HouseholdId, cancellationToken);
    }
}
