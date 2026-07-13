namespace SplitCard.Api.Contracts.Households;

public sealed record RegisterHouseholdRequest(
    string HouseholdName,
    string OwnerName,
    string OwnerEmail,
    string OwnerPassword);

public sealed record RegisterHouseholdResponse(string HouseholdId, string OwnerUserId);
