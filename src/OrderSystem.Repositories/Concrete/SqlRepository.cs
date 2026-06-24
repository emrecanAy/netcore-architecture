using System.Collections;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Repositories.Concrete;

/// <summary>
/// EF Core-backed generic repository. Delegates IQueryable members to the
/// underlying <see cref="DbSet{TEntity}"/> so EF's async LINQ operators work
/// directly on the repository.
/// </summary>
public class SqlRepository<TEntity> : ISQLRepository<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> _set;
    private readonly IQueryable<TEntity> _query;

    public SqlRepository(AppDbContext context)
    {
        _set = context.Set<TEntity>();
        _query = _set;
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await _set.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => _set.Update(entity);

    public void Remove(TEntity entity) => _set.Remove(entity);

    public ValueTask<TEntity?> FindAsync(Guid id, CancellationToken cancellationToken = default) =>
        _set.FindAsync(new object[] { id }, cancellationToken);

    // IQueryable delegation.
    public Type ElementType => _query.ElementType;
    public Expression Expression => _query.Expression;
    public IQueryProvider Provider => _query.Provider;
    public IEnumerator<TEntity> GetEnumerator() => _query.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _query.GetEnumerator();
}
