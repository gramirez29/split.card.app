using SplitCard.Application.Reporting;

namespace SplitCard.Api.Contracts.Reporting;

public sealed record PersonTotalResponse(string PersonId, string PersonName, decimal TotalCrc, decimal TotalUsd)
{
    public static PersonTotalResponse FromApplication(PersonTotal total) =>
        new(total.PersonId, total.PersonName, total.TotalCrc, total.TotalUsd);
}

public sealed record CardTotalResponse(string CardId, string CardName, decimal TotalCrc, decimal TotalUsd)
{
    public static CardTotalResponse FromApplication(CardTotal total) =>
        new(total.CardId, total.CardName, total.TotalCrc, total.TotalUsd);
}

public sealed record HouseholdDashboardResponse(
    decimal GrandTotalCrc,
    decimal GrandTotalUsd,
    IReadOnlyList<PersonTotalResponse> ByPerson,
    IReadOnlyList<CardTotalResponse> ByCard)
{
    public static HouseholdDashboardResponse FromApplication(HouseholdDashboard dashboard) =>
        new(
            dashboard.GrandTotalCrc,
            dashboard.GrandTotalUsd,
            dashboard.ByPerson.Select(PersonTotalResponse.FromApplication).ToList(),
            dashboard.ByCard.Select(CardTotalResponse.FromApplication).ToList());
}
