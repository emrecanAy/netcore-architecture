namespace OrderSystem.Repositories.Abstract;

/// <summary>
/// Generic repository. Derives from <see cref="IQueryable{T}"/> so application
/// handlers can compose EF LINQ (Include/Where/AsNoTracking) directly. This is a
/// pragmatic trade-off discussed in PROJECT.md §15.3.
/// </summary>
public interface ISQLRepository<TEntity> : IQueryable<TEntity> where TEntity : class
{
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
    ValueTask<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default);
}
