using System.Security.Claims;
using SplitCard.Api.Auth;
using SplitCard.Api.Contracts.Cards;
using SplitCard.Application.Cards;

namespace SplitCard.Api.Endpoints;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cards").WithTags("Cards");

        group.MapPost("/", async (
            CreateCardRequest request,
            ClaimsPrincipal actingUser,
            CreateCardCommandHandler handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateCardCommand(
                actingUser.GetUserId(),
                request.HouseholdId,
                request.Name,
                request.Bank,
                request.Type,
                request.CutoffDay,
                request.PaymentDueDay);

            var card = await handler.Handle(command, cancellationToken);

            return Results.Created($"/api/cards/{card.Id}", CardResponse.FromDomain(card));
        })
        .WithName("CreateCard")
        .WithSummary("Owner registers a new credit or debit card for the household.")
        .Produces<CardResponse>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .RequireAuthorization();

        app.MapGroup("/api/households/{householdId}/cards")
            .WithTags("Cards")
            .MapGet("/", async (
                string householdId,
                GetCardsForHouseholdQueryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetCardsForHouseholdQuery(householdId);
                var cards = await handler.Handle(query, cancellationToken);

                return Results.Ok(cards.Select(CardResponse.FromDomain).ToList());
            })
            .WithName("GetCardsForHousehold")
            .WithSummary("Lists every card registered for a household.")
            .Produces<List<CardResponse>>()
            .RequireAuthorization();

        return app;
    }
}
