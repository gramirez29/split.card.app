using SplitCard.Application.Transactions;
using SplitCard.Domain.Entities;
using SplitCard.Domain.Enums;
using SplitCard.Domain.ValueObjects;

namespace SplitCard.Api.Contracts.Transactions;

public sealed record PersonShareRequest(string PersonId, decimal Percentage)
{
    public PersonShare ToDomain() => new(PersonId, Percentage);
}

public sealed record PersonShareResponse(string PersonId, decimal Percentage)
{
    public static PersonShareResponse FromDomain(PersonShare share) => new(share.PersonId, share.Percentage);
}

public sealed record InstallmentPlanRequestInput(int TotalInstallments, decimal InstallmentAmount)
{
    public InstallmentPlanInput ToApplicationInput() => new(TotalInstallments, InstallmentAmount);
}

public sealed record RegisterTransactionRequest(
    string CardId,
    string Merchant,
    DateOnly PurchaseDate,
    decimal Amount,
    Currency Currency,
    InstallmentPlanRequestInput? Installments,
    IReadOnlyList<PersonShareRequest> Split);

public sealed record TransactionResponse(
    string Id,
    string CardId,
    string Merchant,
    DateOnly PurchaseDate,
    decimal Amount,
    Currency Currency,
    string? InstallmentPlanId,
    string CreatedByUserId,
    IReadOnlyList<PersonShareResponse> Split)
{
    public static TransactionResponse FromDomain(Transaction transaction) =>
        new(
            transaction.Id,
            transaction.CardId,
            transaction.Merchant,
            transaction.PurchaseDate,
            transaction.Amount,
            transaction.Currency,
            transaction.InstallmentPlanId,
            transaction.CreatedByUserId,
            transaction.Split.Select(PersonShareResponse.FromDomain).ToList());
}

/// <summary>
/// Used only by period-scoped endpoints (GET .../cards/{cardId}/transactions). Unlike
/// TransactionResponse, `amount` here stays the full original purchase total for context,
/// but `periodAmount` is what's actually due THIS period — for a non-installment purchase
/// they're the same value; for an installment purchase they are NOT, and periodAmount is
/// the one that should be shown/summed by any UI rendering "what's owed this period".
/// </summary>
public sealed record PeriodTransactionResponse(
    string Id,
    string CardId,
    string Merchant,
    DateOnly PurchaseDate,
    decimal Amount,
    decimal PeriodAmount,
    Currency Currency,
    string? InstallmentPlanId,
    int? InstallmentNumber,
    int? TotalInstallments,
    string CreatedByUserId,
    IReadOnlyList<PersonShareResponse> Split)
{
    public static PeriodTransactionResponse FromResolved(ResolvedPeriodTransaction resolved)
    {
        var transaction = resolved.Transaction;

        return new PeriodTransactionResponse(
            transaction.Id,
            transaction.CardId,
            transaction.Merchant,
            transaction.PurchaseDate,
            transaction.Amount,
            resolved.PeriodAmount,
            transaction.Currency,
            transaction.InstallmentPlanId,
            resolved.InstallmentNumber,
            resolved.TotalInstallments,
            transaction.CreatedByUserId,
            transaction.Split.Select(PersonShareResponse.FromDomain).ToList());
    }
}
