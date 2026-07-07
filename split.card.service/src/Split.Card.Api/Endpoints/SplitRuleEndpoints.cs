using System.Security.Claims;
using SplitCard.Api.Auth;
using SplitCard.Api.Contracts.SplitRules;
using SplitCard.Api.Contracts.Transactions;
using SplitCard.Application.SplitRules;

namespace SplitCard.Api.Endpoints;

public static class SplitRuleEndpoints
{
    public static IEndpointRouteBuilder MapSplitRuleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/split-rules")
            .WithTags("SplitRules")
            .MapPost("/", async (
                CreateSplitRuleRequest request,
                ClaimsPrincipal actingUser,
                CreateSplitRuleCommandHandler handler,
                CancellationToken cancellationToken) =>
            {
                var command = new CreateSplitRuleCommand(
                    actingUser.GetUserId(),
                    request.HouseholdId,
                    request.DescriptionPattern,
                    request.DefaultSplit.Select(s => s.ToDomain()).ToList());

                var rule = await handler.Handle(command, cancellationToken);

                return Results.Created($"/api/split-rules/{rule.Id}", SplitRuleResponse.FromDomain(rule));
            })
            .WithName("CreateSplitRule")
            .WithSummary("Owner creates a default split rule matched by merchant description.")
            .Produces<SplitRuleResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        var householdGroup = app.MapGroup("/api/households/{householdId}/split-rules").WithTags("SplitRules");

        householdGroup.MapGet("/", async (
            string householdId,
            GetSplitRulesForHouseholdQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSplitRulesForHouseholdQuery(householdId);
            var rules = await handler.Handle(query, cancellationToken);

            return Results.Ok(rules.Select(SplitRuleResponse.FromDomain).ToList());
        })
        .WithName("GetSplitRulesForHousehold")
        .WithSummary("Lists every split rule configured for a household.")
        .Produces<List<SplitRuleResponse>>()
        .RequireAuthorization();

        householdGroup.MapGet("/suggest", async (
            string householdId,
            string merchant,
            SuggestSplitForMerchantQueryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var query = new SuggestSplitForMerchantQuery(householdId, merchant);
            var suggestedSplit = await handler.Handle(query, cancellationToken);

            return suggestedSplit is null
                ? Results.NoContent()
                : Results.Ok(suggestedSplit.Select(PersonShareResponse.FromDomain).ToList());
        })
        .WithName("SuggestSplitForMerchant")
        .WithSummary("Suggests a default split for a merchant name, based on existing SplitRules. 204 if no rule matches.")
        .Produces<List<PersonShareResponse>>()
        .Produces(StatusCodes.Status204NoContent)
        .RequireAuthorization();

        return app;
    }
}
