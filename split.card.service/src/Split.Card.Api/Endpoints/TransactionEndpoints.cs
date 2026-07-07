using System.Security.Claims;
using SplitCard.Api.Auth;
using SplitCard.Api.Contracts.Transactions;
using SplitCard.Application.Transactions;

namespace SplitCard.Api.Endpoints;

public static class TransactionEndpoints
{
    public static IEndpointRouteBuilder MapTransactionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/transactions")
            .WithTags("Transactions")
            .MapPost("/", async (
                RegisterTransactionRequest request,
                ClaimsPrincipal actingUser,
                RegisterTransactionCommandHandler handler,
                CancellationToken cancellationToken) =>
            {
                var command = new RegisterTransactionCommand(
                    actingUser.GetUserId(),
                    request.CardId,
                    request.Merchant,
                    request.PurchaseDate,
                    request.Amount,
                    request.Currency,
                    request.Installments?.ToApplicationInput(),
                    request.Split.Select(s => s.ToDomain()).ToList());

                var transaction = await handler.Handle(command, cancellationToken);

                return Results.Created($"/api/transactions/{transaction.Id}", TransactionResponse.FromDomain(transaction));
            })
            .WithName("RegisterTransaction")
            .WithSummary("Registers a purchase at the moment it happens. Auto-assigns the statement period for Credit cards.")
            .Produces<TransactionResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        app.MapGroup("/api/households/{householdId}/transactions")
            .WithTags("Transactions")
            .MapGet("/", async (
                string householdId,
                ClaimsPrincipal actingUser,
                GetVisibleTransactionsQueryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetVisibleTransactionsQuery(householdId, actingUser.GetUserId());
                var transactions = await handler.Handle(query, cancellationToken);

                return Results.Ok(transactions.Select(TransactionResponse.FromDomain).ToList());
            })
            .WithName("GetVisibleTransactions")
            .WithSummary("Lists transactions visible to the authenticated user — Owner sees all, everyone else only their own split.")
            .Produces<List<TransactionResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        app.MapGroup("/api/cards/{cardId}/transactions")
            .WithTags("Transactions")
            .MapGet("/", async (
                string cardId,
                DateOnly date,
                ClaimsPrincipal actingUser,
                GetTransactionsForCardPeriodQueryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetTransactionsForCardPeriodQuery(cardId, date, actingUser.GetUserId());
                var transactions = await handler.Handle(query, cancellationToken);

                return Results.Ok(transactions.Select(TransactionResponse.FromDomain).ToList());
            })
            .WithName("GetTransactionsForCardPeriod")
            .WithSummary("Lists transactions in the statement period containing `date` for the given card.")
            .Produces<List<TransactionResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return app;
    }
}
