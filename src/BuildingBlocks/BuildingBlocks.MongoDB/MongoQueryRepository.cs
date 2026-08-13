using System.Linq.Expressions;
using BuildingBlocks.Abstractions;
using BuildingBlocks.Domain;
using MongoDB.Driver;

namespace BuildingBlocks.MongoDB;

public class MongoQueryRepository<TEntity>(IMongoDbContext context) : IQueryRepository<TEntity>
    where TEntity : MongoEntity
{
    protected readonly IMongoCollection<TEntity> Collection =
        context.Database.GetCollection<TEntity>(typeof(TEntity).Name);

    protected IMongoDbContext Context { get; } = context;

    public IQueryable<TTemp> Select<TTemp>() where TTemp : class
    {
        throw new NotSupportedException(
            "MongoDB repository does not support arbitrary generic Select<T> directly like EF.");
    }

    public Task<List<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Find(predicate).ToListAsync(cancellationToken);
    }

    public Task<TEntity?> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Find(predicate).Limit(1).FirstOrDefaultAsync(cancellationToken)!;
    }

    public Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Find(predicate).Limit(1).FirstAsync(cancellationToken);
    }

    public Task<TEntity?> SingleOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Find(predicate).Limit(2).SingleOrDefaultAsync(cancellationToken)!;
    }

    public Task<TEntity> SingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Find(predicate).Limit(2).SingleAsync(cancellationToken);
    }

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        var count = await CountDocumentsAsync(predicate, cancellationToken);
        return checked((int)count);
    }

    public Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return Find(predicate).Limit(1).AnyAsync(cancellationToken);
    }

    public Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<List<TEntity>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return Find(_ => true).ToListAsync(cancellationToken);
    }

    public IQueryable<TEntity> Query()
    {
        return AsQueryable();
    }

    public IQueryable<TEntity> QueryNoTracking()
    {
        return AsQueryable();
    }

    public IQueryable<T> QueryNoTracking<T>() where T : class
    {
        if (typeof(T) == typeof(TEntity)) return (IQueryable<T>)AsQueryable();

        throw new NotSupportedException(
            "QueryNoTracking with different type not fully supported in this generic adapter.");
    }

    public async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate,
        int pageIndex,
        int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null,
        bool isAsc = true,
        CancellationToken cancellationToken = default)
    {
        ValidatePagination(pageIndex, pageSize);
        var skip = checked((pageIndex - 1) * pageSize);
        var session = GetActiveSession();
        var query = Find(predicate, session);

        if (orderBy is not null)
            query = isAsc ? query.SortBy(orderBy) : query.SortByDescending(orderBy);

        if (session is not null)
        {
            // MongoDB does not support parallel operations within the same transaction session.
            var total = await CountDocumentsAsync(predicate, cancellationToken, session);
            var result = await query.Skip(skip).Limit(pageSize).ToListAsync(cancellationToken);
            return (checked((int)total), result);
        }

        var totalTask = CountDocumentsAsync(predicate, cancellationToken);
        var resultTask = query.Skip(skip).Limit(pageSize).ToListAsync(cancellationToken);
        await Task.WhenAll(totalTask, resultTask);

        return (checked((int)await totalTask), await resultTask);
    }

    private IFindFluent<TEntity, TEntity> Find(
        Expression<Func<TEntity, bool>> predicate,
        IClientSessionHandle? session = null)
    {
        session ??= GetActiveSession();
        return session is null ? Collection.Find(predicate) : Collection.Find(session, predicate);
    }

    private Task<long> CountDocumentsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken,
        IClientSessionHandle? session = null)
    {
        session ??= GetActiveSession();
        return session is null
            ? Collection.CountDocumentsAsync(predicate, cancellationToken: cancellationToken)
            : Collection.CountDocumentsAsync(session, predicate, cancellationToken: cancellationToken);
    }

    private IQueryable<TEntity> AsQueryable()
    {
        var session = GetActiveSession();
        return session is null ? Collection.AsQueryable() : Collection.AsQueryable(session);
    }

    private IClientSessionHandle? GetActiveSession()
    {
        return Context.Session is { IsInTransaction: true } session ? session : null;
    }

    private static void ValidatePagination(int pageIndex, int pageSize)
    {
        if (pageIndex <= 0) throw new ArgumentOutOfRangeException(nameof(pageIndex));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));
    }
}