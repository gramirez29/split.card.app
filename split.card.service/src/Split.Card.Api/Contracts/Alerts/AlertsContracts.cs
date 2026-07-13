using SplitCard.Application.Alerts;

namespace SplitCard.Api.Contracts.Alerts;

public sealed record CardAlertResponse(string CardId, string CardName, string AlertType, DateOnly Date, int DaysUntil)
{
    public static CardAlertResponse FromApplication(CardAlert alert) =>
        new(alert.CardId, alert.CardName, alert.AlertType, alert.Date, alert.DaysUntil);
}

public sealed record InstallmentAlertResponse(
    string CardId,
    string CardName,
    string TransactionId,
    string Merchant,
    decimal PeriodAmount,
    SplitCard.Domain.Enums.Currency Currency,
    int InstallmentNumber,
    int TotalInstallments,
    DateOnly PaymentDueDate,
    int DaysUntilPaymentDue)
{
    public static InstallmentAlertResponse FromApplication(InstallmentAlert alert) =>
        new(
            alert.CardId,
            alert.CardName,
            alert.TransactionId,
            alert.Merchant,
            alert.PeriodAmount,
            alert.Currency,
            alert.InstallmentNumber,
            alert.TotalInstallments,
            alert.PaymentDueDate,
            alert.DaysUntilPaymentDue);
}

public sealed record HouseholdAlertsResponse(
    IReadOnlyList<CardAlertResponse> CardAlerts,
    IReadOnlyList<InstallmentAlertResponse> InstallmentAlerts)
{
    public static HouseholdAlertsResponse FromApplication(HouseholdAlerts alerts) =>
        new(
            alerts.CardAlerts.Select(CardAlertResponse.FromApplication).ToList(),
            alerts.InstallmentAlerts.Select(InstallmentAlertResponse.FromApplication).ToList());
}
