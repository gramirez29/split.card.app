using SplitCard.Domain.Entities;

namespace SplitCard.Application.Common;

/// <summary>
/// Every household-scoped Command/Query must call this right after loading the acting
/// User, before doing anything else with the household/card/etc. it's about to touch.
/// Without it, HouseholdId (or a CardId that resolves to one) is fully client-controlled —
/// any authenticated user can read or write another household's data by guessing/enumerating
/// its id, regardless of role checks like Owner-only, which only check the ROLE, not
/// WHICH household the actor belongs to.
/// </summary>
public static class HouseholdAccessGuard
{
    public static void EnsureMember(User actingUser, string householdId)
    {
        if (actingUser.HouseholdId != householdId)
        {
            throw new ForbiddenException("You do not have access to this household.");
        }
    }
}
