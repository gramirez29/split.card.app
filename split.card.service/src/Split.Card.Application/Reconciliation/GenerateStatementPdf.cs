using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Application.Transactions;

namespace SplitCard.Application.Reconciliation;

public sealed record GenerateStatementPdfQuery(string CardId, DateOnly Date, string ActingUserId);

/// <summary>
/// Reuses the exact same guard + visibility logic as GetTransactionsForCardPeriodQuery,
/// and the same PeriodTransactionResolver — so the PDF shows the correct per-installment
/// amount for each period, not the full purchase total. Also resolves PersonId -> Name for
/// every household member so the PDF shows names, not raw ids.
/// </summary>
public sealed class GenerateStatementPdfQueryHandler(
    IUserRepository userRepository,
    ICardRepository cardRepository,
    IStatementPeriodRepository statementPeriodRepository,
    ITransactionRepository transactionRepository,
    IInstallmentPlanRepository installmentPlanRepository,
    IStatementPdfGenerator pdfGenerator)
{
    public async Task<byte[]> Handle(GenerateStatementPdfQuery query, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(query.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {query.ActingUserId} not found.");

        var card = await cardRepository.GetByIdAsync(query.CardId, cancellationToken)
            ?? throw new NotFoundException($"Card {query.CardId} not found.");

        HouseholdAccessGuard.EnsureMember(actingUser, card.HouseholdId);

        var period = await statementPeriodRepository.GetByCardAndDateAsync(query.CardId, query.Date, cancellationToken)
            ?? throw new NotFoundException($"No statement period found for card {query.CardId} on {query.Date}.");

        var resolved = await PeriodTransactionResolver.Resolve(
            query.CardId,
            period.StartDate,
            period.EndDate,
            transactionRepository,
            installmentPlanRepository,
            cancellationToken);

        var visibleTransactions = actingUser.CanReadAll()
            ? resolved
            : resolved.Where(r => r.Transaction.IsVisibleTo(query.ActingUserId)).ToList();

        var members = await userRepository.GetByHouseholdIdAsync(card.HouseholdId, cancellationToken);
        var personNamesById = members.ToDictionary(m => m.Id, m => m.Name);

        var model = new StatementPdfModel(card, period, visibleTransactions, personNamesById);

        return pdfGenerator.Generate(model);
    }
}
