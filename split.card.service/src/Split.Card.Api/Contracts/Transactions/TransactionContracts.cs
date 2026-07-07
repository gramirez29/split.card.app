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
