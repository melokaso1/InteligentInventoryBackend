using Application.Abstractions;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
