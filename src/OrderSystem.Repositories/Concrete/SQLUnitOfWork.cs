using Microsoft.EntityFrameworkCore.Storage;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Repositories.Concrete;

public class SQLUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public SQLUnitOfWork(AppDbContext context) => _context = context;

    public IEnumerable<TEntity> GetChanges<TEntity>() where TEntity : class =>
        _context.ChangeTracker.Entries<TEntity>().Select(e => e.Entity);

    public bool HasChanges() => _context.ChangeTracker.HasChanges();

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        _context.Database.BeginTransactionAsync(cancellationToken);
}
