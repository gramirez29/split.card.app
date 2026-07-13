using SplitCard.Application.Transactions;
using SplitCard.Domain.Entities;

namespace SplitCard.Application.Abstractions;

public sealed record StatementPdfModel(
    Card Card,
    StatementPeriod Period,
    IReadOnlyList<ResolvedPeriodTransaction> Transactions,
    IReadOnlyDictionary<string, string> PersonNamesById);

public interface IStatementPdfGenerator
{
    byte[] Generate(StatementPdfModel model);
}
