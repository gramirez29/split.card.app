using SplitCard.Api.Contracts.Households;
using SplitCard.Application.Households;

namespace SplitCard.Api.Endpoints;

public static class HouseholdEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/households").WithTags("Households");

        group.MapPost("/", async (
            RegisterHouseholdRequest request,
            RegisterHouseholdCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterHouseholdCommand(
                request.HouseholdName,
                request.OwnerName,
                request.OwnerEmail,
                request.OwnerPassword);

            var result = await handler.Handle(command, cancellationToken);

            return Results.Created(
                $"/api/households/{result.HouseholdId}",
                new RegisterHouseholdResponse(result.HouseholdId, result.OwnerUserId));
        })
        .WithName("RegisterHousehold")
        .WithSummary("Bootstraps a new Household with its first Owner user.")
        .Produces<RegisterHouseholdResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        return app;
    }
}
