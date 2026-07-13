using System.Security.Claims;
using SplitCard.Api.Auth;
using SplitCard.Api.Contracts.Alerts;
using SplitCard.Application.Alerts;

namespace SplitCard.Api.Endpoints;

public static class AlertsEndpoints
{
    public static IEndpointRouteBuilder MapAlertsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/households/{householdId}/alerts")
            .WithTags("Alerts")
            .MapGet("/", async (
                string householdId,
                ClaimsPrincipal actingUser,
                GetHouseholdAlertsQueryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetHouseholdAlertsQuery(householdId, actingUser.GetUserId());
                var alerts = await handler.Handle(query, cancellationToken);

                return Results.Ok(HouseholdAlertsResponse.FromApplication(alerts));
            })
            .WithName("GetHouseholdAlerts")
            .WithSummary("In-app alerts: cards with a cutoff or payment due date within 5 days, and which installment purchases are part of an upcoming payment. No push notifications — this is data for the mobile Home tab to render.")
            .Produces<HouseholdAlertsResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization();

        return app;
    }
}
