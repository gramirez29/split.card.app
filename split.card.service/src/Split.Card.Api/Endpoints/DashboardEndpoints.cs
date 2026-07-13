using System.Security.Claims;
using SplitCard.Api.Auth;
using SplitCard.Api.Contracts.Reporting;
using SplitCard.Application.Reporting;

namespace SplitCard.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/households/{householdId}/dashboard")
            .WithTags("Reporting")
            .MapGet("/", async (
                string householdId,
                ClaimsPrincipal actingUser,
                GetHouseholdDashboardQueryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetHouseholdDashboardQuery(householdId, actingUser.GetUserId());
                var dashboard = await handler.Handle(query, cancellationToken);

                return Results.Ok(HouseholdDashboardResponse.FromApplication(dashboard));
            })
            .WithName("GetHouseholdDashboard")
            .WithSummary("Consolidated totals owed right now, by person and by card, for the household's Credit cards' current open period.")
            .Produces<HouseholdDashboardResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization();

        return app;
    }
}
