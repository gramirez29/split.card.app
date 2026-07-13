using MongoDB.Driver;
using SplitCard.Application.Abstractions;
using SplitCard.Domain.Entities;
using SplitCard.Infrastructure.Persistence;
using SplitCard.Infrastructure.Persistence.Mappings;

namespace SplitCard.Infrastructure.Repositories;

public sealed class MongoInstallmentPlanRepository(MongoDbContext context) : IInstallmentPlanRepository
{
    public async Task<InstallmentPlan?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var document = await context.InstallmentPlans.Find(p => p.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    /// <summary>
    /// Adds a new installment plan to the database.
    /// </summary>
    /// <param name="installmentPlan">The installment plan to add in the database.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task AddAsync(InstallmentPlan installmentPlan, CancellationToken cancellationToken)
    {
        await context.InstallmentPlans.InsertOneAsync(installmentPlan.ToDocument(), cancellationToken: cancellationToken);
    }
}
