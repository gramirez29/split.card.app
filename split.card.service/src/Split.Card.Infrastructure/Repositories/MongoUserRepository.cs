using MongoDB.Driver;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence;
using SplitCard.Infrastructure.Persistence.Mappings;

namespace SplitCard.Infrastructure.Repositories;

public sealed class MongoUserRepository(MongoDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var document = await context.Users.Find(u => u.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var document = await context.Users.Find(u => u.Email == email).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyList<User>> GetByHouseholdIdAsync(string householdId, CancellationToken cancellationToken)
    {
        var documents = await context.Users.Find(u => u.HouseholdId == householdId).ToListAsync(cancellationToken);
        return documents.Select(d => d.ToDomain()).ToList();
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.InsertOneAsync(user.ToDocument(), cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        await context.Users.ReplaceOneAsync(u => u.Id == user.Id, user.ToDocument(), cancellationToken: cancellationToken);
    }
}
