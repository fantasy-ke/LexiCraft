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

    public IQueryable<TTemp> Select<TTemp>() where TTemp : class
    {
        throw new NotSupportedException(
            "MongoDB repository does not support arbitrary generic Select<T> directly like EF.");
    }

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Collection.Find(predicate).ToListAsync();
    }

    public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Collection.Find(predicate).FirstOrDefaultAsync()!;
    }

    public Task<TEntity> FirstAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Collection.Find(predicate).FirstAsync();
    }

    public Task<TEntity> SingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Collection.Find(predicate).SingleOrDefaultAsync();
    }

    public Task<TEntity> SingleAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Collection.Find(predicate).SingleAsync();
    }

    public Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Task.FromResult((int)Collection.CountDocuments(predicate));
    }

    public Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return Collection.Find(predicate).AnyAsync();
    }

    public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate)
    {
        return FirstOrDefaultAsync(predicate);
    }

    public Task<List<TEntity>> GetListAsync()
    {
        return Collection.Find(_ => true).ToListAsync();
    }

    public IQueryable<TEntity> Query()
    {
        return Collection.AsQueryable();
    }

    public IQueryable<TEntity> QueryNoTracking()
    {
        return Collection.AsQueryable();
    }

    public IQueryable<T> QueryNoTracking<T>() where T : class
    {
        if (typeof(T) == typeof(TEntity))
        {
            return (IQueryable<T>)Collection.AsQueryable();
        }

        throw new NotSupportedException(
            "QueryNoTracking with different type not fully supported in this generic adapter.");
    }

    public async Task<(int total, IEnumerable<TEntity> result)> GetPageListAsync(
        Expression<Func<TEntity, bool>> predicate, int pageIndex, int pageSize,
        Expression<Func<TEntity, object>>? orderBy = null, bool isAsc = true)
    {
        var findOptions = Collection.Find(predicate);
        var total = (int)await findOptions.CountDocumentsAsync();

        if (orderBy != null)
        {
            findOptions = isAsc ? findOptions.SortBy(orderBy) : findOptions.SortByDescending(orderBy);
        }

        var result = await findOptions.Skip((pageIndex - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        return (total, result);
    }
}
