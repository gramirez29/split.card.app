using SplitCard.Application.Abstractions;
using SplitCard.Application.Common;
using SplitCard.Domain.Common;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace SplitCard.Application.Transactions;

public sealed record RegisterTransactionCommand(
    string ActingUserId,
    string CardId,
    string Merchant,
    DateOnly PurchaseDate,
    decimal Amount,
    Currency Currency,
    InstallmentPlanInput? Installments,
    IReadOnlyList<PersonShare> Split);

/// <summary>
/// The core write path of the app: registers a purchase at the moment it happens.
/// - If Installments is provided, creates a single InstallmentPlan (never one Transaction
///   per month — GetInstallmentNumberFor resolves which installment is active per period).
/// - Debit card purchases cannot carry Installments — a debit purchase is settled in cash
///   at the moment of purchase, there is no such thing as a debit installment plan.
/// - For Credit cards, ensures a StatementPeriod exists for the purchase date (creates it
///   on first purchase of that period, reuses it afterward) via Card.GetStatementPeriodFor.
/// - Debit cards skip StatementPeriod entirely — each Transaction is a completed cash-out.
/// - Split validation (percentages sum to 100, no duplicate PersonId) happens inside the
///   Transaction domain constructor; this handler does not duplicate that logic.
/// </summary>
public sealed class RegisterTransactionCommandHandler(
    IUserRepository userRepository,
    ICardRepository cardRepository,
    IInstallmentPlanRepository installmentPlanRepository,
    IStatementPeriodRepository statementPeriodRepository,
    ITransactionRepository transactionRepository)
{
    public async Task<Transaction> Handle(RegisterTransactionCommand command, CancellationToken cancellationToken)
    {
        var actingUser = await userRepository.GetByIdAsync(command.ActingUserId, cancellationToken)
            ?? throw new NotFoundException($"User {command.ActingUserId} not found.");

        if (!actingUser.CanWrite())
        {
            throw new ForbiddenException("This role cannot register transactions.");
        }

        var card = await cardRepository.GetByIdAsync(command.CardId, cancellationToken)
            ?? throw new NotFoundException($"Card {command.CardId} not found.");

        if (card.Type == CardType.Debit && command.Installments is not null)
        {
            throw new ApplicationValidationException(
                "Debit card purchases cannot be paid in installments — they are settled in cash at the moment of purchase.");
        }

        string? installmentPlanId = null;

        if (command.Installments is not null)
        {
            var plan = InstallmentPlan.CreateForNewPurchase(
                IdGenerator.NewId(),
                command.Installments.TotalInstallments,
                command.Installments.InstallmentAmount,
                command.Currency,
                command.PurchaseDate);

            await installmentPlanRepository.AddAsync(plan, cancellationToken);
            installmentPlanId = plan.Id;
        }

        if (card.Type == CardType.Credit)
        {
            await EnsureStatementPeriodExists(card, command.PurchaseDate, cancellationToken);
        }

        var transaction = new Transaction(
            IdGenerator.NewId(),
            command.CardId,
            command.Merchant,
            command.PurchaseDate,
            command.Amount,
            command.Currency,
            installmentPlanId,
            command.ActingUserId,
            command.Split);

        await transactionRepository.AddAsync(transaction, cancellationToken);

        return transaction;
    }

    private async Task EnsureStatementPeriodExists(Card card, DateOnly purchaseDate, CancellationToken cancellationToken)
    {
        var existingPeriod = await statementPeriodRepository.GetByCardAndDateAsync(card.Id, purchaseDate, cancellationToken);

        if (existingPeriod is not null)
        {
            return;
        }

        var range = card.GetStatementPeriodFor(purchaseDate);
        var period = new StatementPeriod(IdGenerator.NewId(), card.Id, range.StartDate, range.EndDate, range.PaymentDueDate);

        await statementPeriodRepository.AddAsync(period, cancellationToken);
    }
}
