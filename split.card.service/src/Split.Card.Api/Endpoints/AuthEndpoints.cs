using System.Security.Claims;
using SplitCard.Api.Auth;
using SplitCard.Api.Contracts.Auth;
using SplitCard.Api.Contracts.Users;
using SplitCard.Application.Auth;

namespace SplitCard.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/login", async (
            LoginRequest request,
            LoginCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request.Email, request.Password);
            var result = await handler.Handle(command, cancellationToken);

            return Results.Ok(new LoginResponse(result.Token, result.UserId));
        })
        .WithName("Login")
        .WithSummary("Authenticates a user by email/password and returns a JWT.")
        .Produces<LoginResponse>()
        .ProducesProblem(StatusCodes.Status401Unauthorized)
        .AllowAnonymous();

        group.MapGet("/me", async (
            ClaimsPrincipal user,
            GetCurrentUserQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCurrentUserQuery(user.GetUserId());
            var currentUser = await handler.Handle(query, cancellationToken);

            return Results.Ok(UserResponse.FromDomain(currentUser));
        })
        .WithName("GetCurrentUser")
        .WithSummary("Returns the authenticated user's profile.")
        .Produces<UserResponse>()
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        return app;
    }
}
