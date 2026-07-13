using System.Security.Claims;
using SplitCard.Api.Auth;
using SplitCard.Api.Contracts.Users;
using SplitCard.Application.Users;

namespace SplitCard.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapPost("/invite", async (
            InviteUserRequest request,
            ClaimsPrincipal actingUser,
            InviteUserCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new InviteUserCommand(
                actingUser.GetUserId(),
                request.HouseholdId,
                request.Name,
                request.Email,
                request.Password,
                request.Role);

            var user = await handler.Handle(command, cancellationToken);

            return Results.Created($"/api/users/{user.Id}", UserResponse.FromDomain(user));
        })
        .WithName("InviteUser")
        .WithSummary("Owner invites a Contributor or RestrictedViewer into the household.")
        .Produces<UserResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        app.MapGroup("/api/households/{householdId}/members")
            .WithTags("Users")
            .MapGet("/", async (
                string householdId,
                ClaimsPrincipal actingUser,
                GetHouseholdMembersQueryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetHouseholdMembersQuery(householdId, actingUser.GetUserId());
                var members = await handler.Handle(query, cancellationToken);

                return Results.Ok(members.Select(UserResponse.FromDomain).ToList());
            })
            .WithName("GetHouseholdMembers")
            .WithSummary("Lists every member (Owner/Contributor/RestrictedViewer) of a household. Backs the mobile split picker.")
            .Produces<List<UserResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .RequireAuthorization();

        return app;
    }
}
