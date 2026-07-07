using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;

namespace Split.Card.Application.Tests.Fakes;

// NOTE: SplitCard.Domain.Entities.Card is always fully qualified in this file. The
// enclosing namespace (Split.Card.Application.Tests.Fakes) contains a "Card" segment,
// so the bare identifier "Card" resolves to that namespace segment, not the type,
// producing CS0118. Same reason CardTests.cs in Split.Card.Domain.Tests does this.

public sealed class InMemoryUserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _users = [];

    public void Seed(User user) => _users[user.Id] = user;

    public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_users.GetValueOrDefault(id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(_users.Values.FirstOrDefault(u => u.Email == email));

    public Task<IReadOnlyList<User>> GetByHouseholdIdAsync(string householdId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<User>>(_users.Values.Where(u => u.HouseholdId == householdId).ToList());

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        _users[user.Id] = user;
        return Task.CompletedTask;
    }
}

public sealed class InMemoryCardRepository : ICardRepository
{
    private readonly Dictionary<string, SplitCard.Domain.Entities.Card> _cards = [];

    public void Seed(SplitCard.Domain.Entities.Card card) => _cards[card.Id] = card;

    public Task<SplitCard.Domain.Entities.Card?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(_cards.GetValueOrDefault(id));

    public Task<IReadOnlyList<SplitCard.Domain.Entities.Card>> GetByHouseholdIdAsync(string householdId, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SplitCard.Domain.Entities.Card>>(_cards.Values.Where(c => c.HouseholdId == householdId).ToList());

    public Task AddAsync(SplitCard.Domain.Entities.Card card, CancellationToken cancellationToken)
    {
        _cards[card.Id] = card;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(SplitCard.Domain.Entities.Card card, CancellationToken cancellationToken)
    {
        _cards[card.Id] = card;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        _cards.Remove(id);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryInstallmentPlanRepository : IInstallmentPlanRepository
{
    public readonly List<InstallmentPlan> Plans = [];

    public Task<InstallmentPlan?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(Plans.FirstOrDefault(p => p.Id == id));

    public Task AddAsync(InstallmentPlan installmentPlan, CancellationToken cancellationToken)
    {
        Plans.Add(installmentPlan);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryStatementPeriodRepository : IStatementPeriodRepository
{
    public readonly List<StatementPeriod> Periods = [];

    public Task<StatementPeriod?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(Periods.FirstOrDefault(p => p.Id == id));

    public Task<StatementPeriod?> GetByCardAndDateAsync(string cardId, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Periods.FirstOrDefault(p => p.CardId == cardId && p.StartDate <= date && p.EndDate >= date));

    public Task AddAsync(StatementPeriod statementPeriod, CancellationToken cancellationToken)
    {
        Periods.Add(statementPeriod);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(StatementPeriod statementPeriod, CancellationToken cancellationToken)
    {
        var index = Periods.FindIndex(p => p.Id == statementPeriod.Id);

        if (index >= 0)
        {
            Periods[index] = statementPeriod;
        }

        return Task.CompletedTask;
    }
}

public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    public readonly List<Transaction> Transactions = [];

    public Task<Transaction?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
        Task.FromResult(Transactions.FirstOrDefault(t => t.Id == id));

    public Task<IReadOnlyList<Transaction>> GetByCardAndDateRangeAsync(
        string cardId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        var results = Transactions
            .Where(t => t.CardId == cardId && t.PurchaseDate >= startDate && t.PurchaseDate <= endDate)
            .ToList();

        return Task.FromResult<IReadOnlyList<Transaction>>(results);
    }

    public Task<IReadOnlyList<Transaction>> GetVisibleToUserAsync(
        string householdId,
        string userId,
        bool isOwner,
        CancellationToken cancellationToken)
    {
        var results = isOwner
            ? Transactions
            : Transactions.Where(t => t.IsVisibleTo(userId)).ToList();

        return Task.FromResult<IReadOnlyList<Transaction>>(results);
    }

    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        Transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Transaction transaction, CancellationToken cancellationToken)
    {
        var index = Transactions.FindIndex(t => t.Id == transaction.Id);

        if (index >= 0)
        {
            Transactions[index] = transaction;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        Transactions.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }
}
