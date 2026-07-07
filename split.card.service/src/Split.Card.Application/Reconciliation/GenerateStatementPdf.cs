using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;

namespace SplitCard.Application.Reconciliation;

public sealed record GenerateStatementPdfQuery(string CardId, DateOnly Date, string ActingUserId);

/// <summary>
/// Reuses the exact same guard + visibility logic as GetTransactionsForCardPeriodQuery
/// (household membership via the loaded Card, then Split[].PersonId filtering for
/// non-Owners) before handing the data to IStatementPdfGenerator. Also resolves
/// PersonId -> Name for every household member so the PDF shows names, not raw ids —
/// a reconciliation document with bare ids is useless to a human.
/// </summary>
public sealed class GenerateStatementPdfQueryHandler(
    IUserRepository userRepository,
    ICardRepository cardRepository,
    IStatementPeriodRepository statementPeriodRepository,
    ITransactionRepository transactionRepository,
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

        var transactions = await transactionRepository.GetByCardAndDateRangeAsync(
            query.CardId,
            period.StartDate,
            period.EndDate,
            cancellationToken);

        var visibleTransactions = actingUser.CanReadAll()
            ? transactions
            : transactions.Where(t => t.IsVisibleTo(query.ActingUserId)).ToList();

        var members = await userRepository.GetByHouseholdIdAsync(card.HouseholdId, cancellationToken);
        var personNamesById = members.ToDictionary(m => m.Id, m => m.Name);

        var model = new StatementPdfModel(card, period, visibleTransactions, personNamesById);

        return pdfGenerator.Generate(model);
    }
}
