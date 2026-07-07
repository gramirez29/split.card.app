using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.Transactions;

public sealed record GetVisibleTransactionsQuery(string HouseholdId, string ActingUserId);

/// <summary>
/// Applies the Split[].PersonId visibility rule at the repository/query level: Owner sees
/// every transaction in the household, everyone else only sees the ones where they appear
/// in the split — regardless of who registered it (Opción B, decided in the domain design).
/// </summary>
public sealed class GetVisibleTransactionsQueryHandler(
    IUserRepository userRepository,
    ITransactionRepository transactionRepository)
{
    public async Task<IReadOnlyList<Transaction>> Handle(GetVisibleTransactionsQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        return await transactionRepository.GetVisibleToUserAsync(
            query.HouseholdId,
            query.ActingUserId,
            actingUser.CanReadAll(),
            cancellationToken);
    }
}
