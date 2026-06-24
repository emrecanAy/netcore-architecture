using Microsoft.EntityFrameworkCore.Storage;

namespace OrderSystem.Repositories.Abstract;

/// <summary>
/// Thin shell over EF Core's ChangeTracker. Lets the pipeline collect tracked
/// entities (to harvest domain events), commit once, and wrap everything in a
/// single transaction (PROJECT.md §8.2, §15.5).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>Tracked entities assignable to <typeparamref name="TEntity"/> (e.g. a base type).</summary>
    IEnumerable<TEntity> GetChanges<TEntity>() where TEntity : class;

    bool HasChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
