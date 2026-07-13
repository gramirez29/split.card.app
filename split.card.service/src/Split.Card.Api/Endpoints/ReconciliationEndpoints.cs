using System.Security.Claims;
using SplitCard.Api.Auth;
using SplitCard.Application.Reconciliation;

namespace SplitCard.Api.Endpoints;

public static class ReconciliationEndpoints
{
    public static IEndpointRouteBuilder MapReconciliationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/cards/{cardId}/statement-pdf")
            .WithTags("Reconciliation")
            .MapGet("/", async (
                string cardId,
                DateOnly date,
                ClaimsPrincipal actingUser,
                GenerateStatementPdfQueryHandler handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GenerateStatementPdfQuery(cardId, date, actingUser.GetUserId());
                var pdfBytes = await handler.Handle(query, cancellationToken);

                return Results.File(pdfBytes, "application/pdf", $"statement-{cardId}-{date:yyyy-MM-dd}.pdf");
            })
            .WithName("GenerateStatementPdf")
            .WithSummary("Generates a PDF of the transactions in the statement period containing `date`, for reconciliation against the real bank statement.")
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireAuthorization();

        return app;
    }
}
