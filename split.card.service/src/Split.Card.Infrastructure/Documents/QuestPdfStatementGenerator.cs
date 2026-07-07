using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Enums;

namespace SplitCard.Infrastructure.Documents;

/// <summary>
/// QuestPDF Community license — free for this use case (single company, revenue under
/// QuestPDF's published threshold). If that ever changes, the license line below is the
/// only place that needs updating.
/// </summary>
public sealed class QuestPdfStatementGenerator : IStatementPdfGenerator
{
    static QuestPdfStatementGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(StatementPdfModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(model.Card.Name).FontSize(18).Bold();
                    column.Item().Text($"{model.Card.Bank} — {model.Card.Type}");
                    column.Item().Text($"Statement period: {model.Period.StartDate:yyyy-MM-dd} to {model.Period.EndDate:yyyy-MM-dd}");
                    column.Item().Text($"Payment due: {model.Period.PaymentDueDate:yyyy-MM-dd}");
                });

                page.Content().PaddingVertical(15).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(4);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Merchant").Bold();
                        header.Cell().Text("Date").Bold();
                        header.Cell().Text("Amount").Bold();
                        header.Cell().Text("Split").Bold();
                    });

                    foreach (var transaction in model.Transactions.OrderBy(t => t.PurchaseDate))
                    {
                        var splitDescription = string.Join(
                            ", ",
                            transaction.Split.Select(share =>
                            {
                                var personName = model.PersonNamesById.GetValueOrDefault(share.PersonId, share.PersonId);
                                return $"{personName}: {share.Percentage}%";
                            }));

                        table.Cell().Text(transaction.Merchant);
                        table.Cell().Text(transaction.PurchaseDate.ToString("yyyy-MM-dd"));
                        table.Cell().Text(FormatAmount(transaction.Amount, transaction.Currency));
                        table.Cell().Text(splitDescription);
                    }
                });

                page.Footer().Column(column =>
                {
                    var totalsByCurrency = model.Transactions
                        .GroupBy(t => t.Currency)
                        .Select(group => FormatAmount(group.Sum(t => t.Amount), group.Key));

                    column.Item().AlignRight().Text($"Total: {string.Join("  +  ", totalsByCurrency)}").Bold();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatAmount(decimal amount, Currency currency) =>
        currency == Currency.CRC
            ? $"₡{amount:N0}"
            : $"${amount:N2}";
}
